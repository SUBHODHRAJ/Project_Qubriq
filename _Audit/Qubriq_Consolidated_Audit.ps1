#requires -Version 5.1
<#
Qubriq consolidated pre-3D audit
- PowerShell 5.1 compatible
- Does NOT delete files, database rows, users, backups, or Git history.
- Creates retained test accounts through the application's Admin APIs.
- Does NOT create OperationsDispatcher unless a verified provisioning endpoint is
  explicitly implemented; this version performs source inspection only.
- Secrets are not written to the report. Generated test passwords are stored in
  a DPAPI-encrypted file readable by the current Windows user on this machine.
#>

[CmdletBinding()]
param(
    [string]$ProjectRoot = "S:\Project_Qubriq",
    [string]$ApiBase = "http://127.0.0.1:5066",
    [string]$FrontendBase = "http://127.0.0.1:5173",
    [switch]$SkipAccountProvisioning
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$AuditDir = Join-Path $ProjectRoot "_Audit"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$ReportPath = Join-Path $AuditDir "Qubriq_Consolidated_Audit_$Timestamp.txt"
$CredentialPath = Join-Path $AuditDir "Qubriq_Autotest_Credentials_$Timestamp.clixml"
$ApiPath = Join-Path $ProjectRoot "backend\Qubriq.API"
$FrontendPath = Join-Path $ProjectRoot "frontend"

if (-not (Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
    throw "Project root not found: $ProjectRoot"
}
New-Item -ItemType Directory -Path $AuditDir -Force | Out-Null

$script:Results = New-Object System.Collections.Generic.List[object]
$script:CredentialRecords = New-Object System.Collections.Generic.List[object]
$script:ReportLines = New-Object System.Collections.Generic.List[string]
$script:AdminToken = $null
$script:AdminUser = $null
$script:TestCustomerId = $null
$script:CustomerToken = $null
$script:DeliveryToken = $null
$script:FinalRecipientToken = $null
$script:TestEmails = @{}

function Write-Audit {
    param([string]$Message, [ConsoleColor]$Color = [ConsoleColor]::Gray)
    $line = "[{0}] {1}" -f (Get-Date -Format "HH:mm:ss"), $Message
    Write-Host $line -ForegroundColor $Color
    $script:ReportLines.Add($line)
}

function Add-Result {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Detail
    )
    $script:Results.Add([pscustomobject]@{
        Test = $Name
        Status = $Status
        Detail = $Detail
    })
    $message = ("{0}: {1} - {2}" -f $Status, $Name, $Detail)
    if ($Status -eq "PASS") {
        Write-Audit $message Green
    } elseif ($Status -eq "FAIL") {
        Write-Audit $message Red
    } else {
        Write-Audit $message Yellow
    }
}

function Invoke-Http {
    param(
        [Parameter(Mandatory=$true)][string]$Method,
        [Parameter(Mandatory=$true)][string]$Url,
        [string]$Token,
        [object]$Body,
        [int[]]$ExpectedStatus = @(200),
        [string]$TestName = "HTTP request"
    )

    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        $headers["Authorization"] = "Bearer $Token"
    }

    $params = @{
        Uri = $Url
        Method = $Method
        Headers = $headers
        UseBasicParsing = $true
        TimeoutSec = 20
        ErrorAction = "Stop"
    }
    if ($null -ne $Body) {
        $params["ContentType"] = "application/json"
        $params["Body"] = ($Body | ConvertTo-Json -Depth 12 -Compress)
    }

    try {
        $response = Invoke-WebRequest @params
        $status = [int]$response.StatusCode
        $raw = [string]$response.Content
    }
    catch {
        $status = 0
        $raw = ""
        if ($_.Exception.Response) {
            try {
                $status = [int]$_.Exception.Response.StatusCode
                $stream = $_.Exception.Response.GetResponseStream()
                if ($stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $raw = $reader.ReadToEnd()
                    $reader.Dispose()
                }
            } catch {}
        }
        if ($status -eq 0) {
            Add-Result $TestName "BLOCKED" ("Request failed: " + $_.Exception.Message)
            return [pscustomobject]@{ StatusCode = 0; Data = $null; Raw = ""; Success = $false }
        }
    }

    $data = $null
    if (-not [string]::IsNullOrWhiteSpace($raw)) {
        try { $data = $raw | ConvertFrom-Json } catch { $data = $null }
    }

    $ok = $ExpectedStatus -contains $status
    if ($ok) {
        Add-Result $TestName "PASS" ("HTTP " + $status)
    } else {
        Add-Result $TestName "FAIL" ("Expected HTTP " + ($ExpectedStatus -join "/") + "; received " + $status)
    }

    return [pscustomobject]@{
        StatusCode = $status
        Data = $data
        Raw = $raw
        Success = $ok
    }
}

function New-TestPassword {
    # Random, high-entropy password; only held in memory and DPAPI-protected on disk.
    $bytes = New-Object byte[] 28
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $rng.Dispose()
    $encoded = [Convert]::ToBase64String($bytes).Replace("+","A").Replace("/","b").Replace("=","")
    return ("Qx9!" + $encoded + "z7")
}

function Convert-SecureToPlain {
    param([Security.SecureString]$Secure)
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

function Save-TestCredential {
    param([string]$Role, [string]$Email, [string]$Password)
    $secure = ConvertTo-SecureString -String $Password -AsPlainText -Force
    $script:CredentialRecords.Add([pscustomobject]@{
        Role = $Role
        Email = $Email
        Password = $secure
        CreatedAt = (Get-Date).ToString("o")
    })
}

function Get-Token {
    param([string]$Email, [string]$Password, [string]$Label)
    $login = Invoke-Http -Method POST -Url "$ApiBase/api/auth/login" `
        -Body @{ email = $Email; password = $Password } `
        -ExpectedStatus @(200) -TestName "$Label login"
    if ($login.Success -and $login.Data -and $login.Data.accessToken) {
        return [string]$login.Data.accessToken
    }
    return $null
}

function Find-OperationsDispatcherProvisioning {
    Write-Audit "Inspecting source for OperationsDispatcher provisioning..." Cyan
    $controllerRoot = Join-Path $ApiPath "Controllers"
    $candidateMatches = @()
    if (Test-Path $controllerRoot) {
        $files = Get-ChildItem -LiteralPath $controllerRoot -Filter "*.cs" -File -Recurse
        foreach ($file in $files) {
            $text = Get-Content -LiteralPath $file.FullName -Raw
            if ($text -match 'OperationsDispatcher' -and
                $text -match '\[HttpPost' -and
                ($text -match 'UserRoles|RoleId|AssignRole|CreateUser|Create.*Account')) {
                $candidateMatches += [pscustomobject]@{
                    File = $file.FullName
                    HasHttpPost = ($text -match '\[HttpPost')
                    HasRoleReference = ($text -match 'OperationsDispatcher')
                    HasUserRoleWrite = ($text -match 'UserRoles|RoleId|AssignRole')
                    HasProvisioningTerms = ($text -match 'CreateUser|Create.*Account|AssignRole')
                }
            }
        }
    }

    if ($candidateMatches.Count -gt 0) {
        foreach ($m in $matches) {
            Add-Result "OperationsDispatcher source candidate" "REVIEW" (
                "Candidate file: $($m.File). Manual route/authorization/transaction review required; no account created."
            )
        }
    } else {
        Add-Result "OperationsDispatcher provisioning discovery" "BLOCKED" (
            "No controller matched POST + OperationsDispatcher + role/user provisioning patterns. No direct SQL role assignment attempted."
        )
    }

    $seedFile = Join-Path $ApiPath "Migrations\Seed_Rbac_Roles_Permissions.sql"
    if (Test-Path $seedFile) {
        $seed = Get-Content -LiteralPath $seedFile -Raw
        if ($seed -match "'OperationsDispatcher'") {
            Add-Result "OperationsDispatcher role seed" "PASS" "Role is referenced in RBAC seed SQL; this does not prove an account provisioning endpoint exists."
        } else {
            Add-Result "OperationsDispatcher role seed" "REVIEW" "Seed file exists but the role name was not found."
        }
    } else {
        Add-Result "OperationsDispatcher role seed" "REVIEW" "Seed SQL file not found at expected path; source scan only."
    }
}

Write-Audit "QUBRIQ CONSOLIDATED AUDIT STARTED" Cyan
Write-Audit "Root: $ProjectRoot"
Write-Audit "API: $ApiBase | Frontend: $FrontendBase"
Write-Audit "Safety: no deletes, no SQL writes, no modifications to existing accounts or Git history." Green

# 1. Git and protected-file inventory
try {
    $git = git -C $ProjectRoot status --short --branch 2>&1
    Add-Result "Git status readable" "PASS" (($git | Out-String).Trim() -replace "`r?`n"," | ")
} catch {
    Add-Result "Git status readable" "REVIEW" $_.Exception.Message
}

$protected = @(
    (Join-Path $ProjectRoot "PHASE1_SOURCE_AUDIT.txt"),
    (Join-Path $ProjectRoot "_Backups\qubriq_dev_backup_20261124_110950.sql"),
    (Join-Path $ProjectRoot "qubriq_dev_backup_20261124_110950.sql")
)
foreach ($path in $protected) {
    if (Test-Path -LiteralPath $path) {
        Add-Result "Protected artifact present" "PASS" ([IO.Path]::GetFileName($path))
    }
}

# 2. Static config and Dispatcher source inspection
$viteConfig = Join-Path $FrontendPath "vite.config.js"
if (Test-Path $viteConfig) {
    $viteText = Get-Content -LiteralPath $viteConfig -Raw
    if ($viteText -match '127\.0\.0\.1:5066') {
        Add-Result "Vite proxy target" "PASS" "Proxy references API port 5066."
    } elseif ($viteText -match '127\.0\.0\.1:5080') {
        Add-Result "Vite proxy target" "FAIL" "Proxy still references 5080 while audit API default is 5066. Update vite.config.js and restart Vite."
    } else {
        Add-Result "Vite proxy target" "REVIEW" "Could not identify the proxy target automatically."
    }
} else {
    Add-Result "Vite proxy target" "BLOCKED" "vite.config.js not found."
}

Find-OperationsDispatcherProvisioning

# 3. Build checks
if (Test-Path (Join-Path $ApiPath "Qubriq.API.csproj")) {
    Push-Location $ApiPath
    try {
        Write-Audit "Building .NET API..." Cyan
        dotnet build --no-restore
        if ($LASTEXITCODE -eq 0) { Add-Result ".NET build" "PASS" "dotnet build --no-restore" }
        else { Add-Result ".NET build" "FAIL" "dotnet build exit code $LASTEXITCODE" }
    } catch {
        Add-Result ".NET build" "FAIL" $_.Exception.Message
    } finally { Pop-Location }
} else {
    Add-Result ".NET build" "BLOCKED" "Qubriq.API.csproj not found."
}

if (Test-Path (Join-Path $FrontendPath "package.json")) {
    Push-Location $FrontendPath
    try {
        Write-Audit "Building React frontend..." Cyan
        npm run build
        if ($LASTEXITCODE -eq 0) { Add-Result "Frontend build" "PASS" "npm run build" }
        else { Add-Result "Frontend build" "FAIL" "npm run build exit code $LASTEXITCODE" }
    } catch {
        Add-Result "Frontend build" "FAIL" $_.Exception.Message
    } finally { Pop-Location }
} else {
    Add-Result "Frontend build" "BLOCKED" "frontend/package.json not found."
}

# 4. Runtime health checks
$rootCheck = Invoke-Http -Method GET -Url "$ApiBase/" -ExpectedStatus @(200) -TestName "API root"
$healthCheck = Invoke-Http -Method GET -Url "$ApiBase/api/health" -ExpectedStatus @(200) -TestName "API health"
$dbCheck = Invoke-Http -Method GET -Url "$ApiBase/api/database/health" -ExpectedStatus @(200) -TestName "MySQL connectivity"
$frontCheck = Invoke-Http -Method GET -Url $FrontendBase -ExpectedStatus @(200) -TestName "Frontend HTTP"

# 5. Authentication and RBAC checks
$adminEmail = Read-Host "Enter the existing Admin email (existing account will not be changed)"
$adminSecure = Read-Host "Enter the existing Admin password" -AsSecureString
$adminPassword = Convert-SecureToPlain $adminSecure
$script:AdminToken = Get-Token -Email $adminEmail -Password $adminPassword -Label "Existing Admin"
$adminPassword = $null
$adminSecure.Dispose()

if ($script:AdminToken) {
    $script:AdminUser = $true

    Invoke-Http -Method GET -Url "$ApiBase/api/customers" -ExpectedStatus @(200) `
        -Token $script:AdminToken -TestName "Admin can list customers" | Out-Null
    Invoke-Http -Method GET -Url "$ApiBase/api/dispatcher/dashboard" -ExpectedStatus @(200) `
        -Token $script:AdminToken -TestName "Admin can access dispatcher dashboard" | Out-Null
    Invoke-Http -Method GET -Url "$ApiBase/api/fleet/vehicles" -ExpectedStatus @(200) `
        -Token $script:AdminToken -TestName "Admin can read fleet vehicles" | Out-Null
    Invoke-Http -Method GET -Url "$ApiBase/api/recipient/shipments" -ExpectedStatus @(403) `
        -Token $script:AdminToken -TestName "Admin blocked from recipient-only endpoint" | Out-Null
} else {
    Add-Result "Admin-protected endpoint tests" "BLOCKED" "Admin login failed; no provisioning requests will be made."
}

# Anonymous checks are safe and do not mutate state.
Invoke-Http -Method GET -Url "$ApiBase/api/customers" -ExpectedStatus @(401) `
    -TestName "Anonymous blocked from customer list" | Out-Null
Invoke-Http -Method GET -Url "$ApiBase/api/dispatcher/dashboard" -ExpectedStatus @(401) `
    -TestName "Anonymous blocked from dispatcher dashboard" | Out-Null
Invoke-Http -Method GET -Url "$ApiBase/api/recipient/shipments" -ExpectedStatus @(401) `
    -TestName "Anonymous blocked from recipient shipments" | Out-Null

# 6. Provision retained test accounts through Admin APIs.
if (-not $SkipAccountProvisioning -and $script:AdminToken) {
    $suffix = (Get-Date -Format "yyyyMMddHHmmss") + "_" + (Get-Random -Minimum 1000 -Maximum 9999)
    $customerEmail = "qubriq_autotest_customer_$suffix@example.test"
    $deliveryEmail = "qubriq_autotest_delivery_$suffix@example.test"
    $finalEmail = "qubriq_autotest_final_$suffix@example.test"

    $customerPassword = New-TestPassword
    $deliveryPassword = New-TestPassword
    $finalPassword = New-TestPassword

    # Create customer profile, then create linked BookingCustomer account.
    $customerProfile = Invoke-Http -Method POST -Url "$ApiBase/api/customers" `
        -Token $script:AdminToken `
        -Body @{
            companyName = "QUBRIQ_AUTOTEST_$suffix"
            contactName = "Qubriq Automated Test"
            email = $customerEmail
            phone = "9000000000"
            address = "Automated test profile - retained"
        } -ExpectedStatus @(201) -TestName "Create disposable customer profile"

    if ($customerProfile.Success -and $customerProfile.Data.id) {
        $script:TestCustomerId = [long]$customerProfile.Data.id
        $customerAccount = Invoke-Http -Method POST -Url "$ApiBase/api/admin/customers/$($script:TestCustomerId)/account" `
            -Token $script:AdminToken `
            -Body @{ fullName = "Qubriq Automated Customer"; email = $customerEmail; password = $customerPassword } `
            -ExpectedStatus @(200) -TestName "Provision BookingCustomer account"
        if ($customerAccount.Success) {
            Save-TestCredential "BookingCustomer" $customerEmail $customerPassword
            $script:TestEmails["BookingCustomer"] = $customerEmail
            $script:CustomerToken = Get-Token -Email $customerEmail -Password $customerPassword -Label "New BookingCustomer"
            if ($script:CustomerToken) {
                Invoke-Http -Method GET -Url "$ApiBase/api/bookings" -Token $script:CustomerToken `
                    -ExpectedStatus @(200) -TestName "BookingCustomer can read own bookings" | Out-Null
                Invoke-Http -Method GET -Url "$ApiBase/api/bookings/customers" -Token $script:CustomerToken `
                    -ExpectedStatus @(403) -TestName "BookingCustomer blocked from all-customer listing" | Out-Null
                Invoke-Http -Method GET -Url "$ApiBase/api/customers" -Token $script:CustomerToken `
                    -ExpectedStatus @(403) -TestName "BookingCustomer blocked from Admin customer API" | Out-Null
                Invoke-Http -Method GET -Url "$ApiBase/api/dispatcher/dashboard" -Token $script:CustomerToken `
                    -ExpectedStatus @(403) -TestName "BookingCustomer blocked from dispatcher dashboard" | Out-Null
            }
        }
    } else {
        Add-Result "BookingCustomer account provisioning" "BLOCKED" "Customer profile creation failed; account was not attempted."
    }

    # Create driver through the Fleet API, then create DeliveryPersonnel linked to it.
    $driverSuffix = $suffix.Replace("_","")
    $driverResponse = Invoke-Http -Method POST -Url "$ApiBase/api/fleet/drivers" `
        -Token $script:AdminToken `
        -Body @{
            fullName = "Qubriq Automated Driver $suffix"
            phone = "9000000001"
            licenseNumber = "QAT$driverSuffix"
        } -ExpectedStatus @(201) -TestName "Create disposable driver for DeliveryPersonnel"
    if ($driverResponse.Success -and $driverResponse.Data.id) {
        $driverId = [long]$driverResponse.Data.id
        $deliveryAccount = Invoke-Http -Method POST -Url "$ApiBase/api/admin/recipients" `
            -Token $script:AdminToken `
            -Body @{
                fullName = "Qubriq Automated Delivery"
                email = $deliveryEmail
                password = $deliveryPassword
                recipientType = "DeliveryPersonnel"
                driverId = $driverId
            } -ExpectedStatus @(201) -TestName "Provision DeliveryPersonnel account"
        if ($deliveryAccount.Success) {
            Save-TestCredential "DeliveryPersonnel" $deliveryEmail $deliveryPassword
            $script:TestEmails["DeliveryPersonnel"] = $deliveryEmail
            $script:DeliveryToken = Get-Token -Email $deliveryEmail -Password $deliveryPassword -Label "New DeliveryPersonnel"
            if ($script:DeliveryToken) {
                Invoke-Http -Method GET -Url "$ApiBase/api/recipient/shipments" -Token $script:DeliveryToken `
                    -ExpectedStatus @(200) -TestName "DeliveryPersonnel can read assigned shipments" | Out-Null
                Invoke-Http -Method GET -Url "$ApiBase/api/dispatcher/dashboard" -Token $script:DeliveryToken `
                    -ExpectedStatus @(403) -TestName "DeliveryPersonnel blocked from dispatcher dashboard" | Out-Null
                Invoke-Http -Method GET -Url "$ApiBase/api/fleet/vehicles" -Token $script:DeliveryToken `
                    -ExpectedStatus @(403) -TestName "DeliveryPersonnel blocked from fleet API" | Out-Null
            }
        }
    } else {
        Add-Result "DeliveryPersonnel provisioning" "BLOCKED" "Driver creation failed; no recipient account attempted."
    }

    # Create FinalRecipient without a driver.
    $finalAccount = Invoke-Http -Method POST -Url "$ApiBase/api/admin/recipients" `
        -Token $script:AdminToken `
        -Body @{
            fullName = "Qubriq Automated Final Recipient"
            email = $finalEmail
            password = $finalPassword
            recipientType = "FinalRecipient"
            driverId = $null
        } -ExpectedStatus @(201) -TestName "Provision FinalRecipient account"
    if ($finalAccount.Success) {
        Save-TestCredential "FinalRecipient" $finalEmail $finalPassword
        $script:TestEmails["FinalRecipient"] = $finalEmail
        $script:FinalRecipientToken = Get-Token -Email $finalEmail -Password $finalPassword -Label "New FinalRecipient"
        if ($script:FinalRecipientToken) {
            Invoke-Http -Method GET -Url "$ApiBase/api/recipient/shipments" -Token $script:FinalRecipientToken `
                -ExpectedStatus @(200) -TestName "FinalRecipient can read incoming shipments" | Out-Null
            Invoke-Http -Method GET -Url "$ApiBase/api/dispatcher/dashboard" -Token $script:FinalRecipientToken `
                -ExpectedStatus @(403) -TestName "FinalRecipient blocked from dispatcher dashboard" | Out-Null
            Invoke-Http -Method GET -Url "$ApiBase/api/fleet/vehicles" -Token $script:FinalRecipientToken `
                -ExpectedStatus @(403) -TestName "FinalRecipient blocked from fleet API" | Out-Null
        }
    }

    $customerPassword = $null
    $deliveryPassword = $null
    $finalPassword = $null
} elseif ($SkipAccountProvisioning) {
    Add-Result "Test account provisioning" "SKIPPED" "SkipAccountProvisioning switch supplied."
}

# 7. Persist generated test credentials securely (no plaintext).
if ($script:CredentialRecords.Count -gt 0) {
    try {
        $script:CredentialRecords | Export-Clixml -LiteralPath $CredentialPath -Force
        Add-Result "Test credentials protected" "PASS" "DPAPI-encrypted credentials saved to $CredentialPath (current Windows user only)."
    } catch {
        Add-Result "Test credentials protected" "FAIL" ("Could not save encrypted credentials: " + $_.Exception.Message)
    }
} else {
    Add-Result "Test credentials protected" "REVIEW" "No new test credentials were generated."
}

# 8. Final report; do not include tokens or passwords.
Write-Audit "========== AUDIT SUMMARY ==========" Cyan
$pass = @($script:Results | Where-Object Status -eq "PASS").Count
$fail = @($script:Results | Where-Object Status -eq "FAIL").Count
$blocked = @($script:Results | Where-Object Status -eq "BLOCKED").Count
$review = @($script:Results | Where-Object Status -eq "REVIEW").Count
$skipped = @($script:Results | Where-Object Status -eq "SKIPPED").Count

Write-Audit "PASS=$pass | FAIL=$fail | BLOCKED=$blocked | REVIEW=$review | SKIPPED=$skipped" Cyan
Write-Audit "Test accounts, if provisioned, are intentionally retained."
Write-Audit "No source files, build folders, database rows, existing accounts, backups, or Git history were deleted."
Write-Audit "OperationsDispatcher account creation was not attempted; inspect candidate source and confirm a supported endpoint first."
Write-Audit "Report: $ReportPath"
Write-Audit "Credentials file (encrypted): $CredentialPath"

$summary = @()
$summary += "QUBRIQ CONSOLIDATED AUDIT"
$summary += "Generated: $(Get-Date -Format o)"
$summary += "Project: $ProjectRoot"
$summary += "API: $ApiBase"
$summary += "Frontend: $FrontendBase"
$summary += "PASS=$pass FAIL=$fail BLOCKED=$blocked REVIEW=$review SKIPPED=$skipped"
$summary += ""
$summary += "RESULTS"
$summary += ($script:Results | Format-Table -AutoSize | Out-String)
$summary += ""
$summary += "EVENT LOG"
$summary += ($script:ReportLines -join [Environment]::NewLine)
$summary | Set-Content -LiteralPath $ReportPath -Encoding UTF8

Write-Host ""
Write-Host "AUDIT FINISHED" -ForegroundColor Green
Write-Host "Report: $ReportPath" -ForegroundColor Cyan
if (Test-Path $CredentialPath) {
    Write-Host "Encrypted test credentials: $CredentialPath" -ForegroundColor Cyan
}
Write-Host "No cleanup/deletion was performed." -ForegroundColor Yellow

