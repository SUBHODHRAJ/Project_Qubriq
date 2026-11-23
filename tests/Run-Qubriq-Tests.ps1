# ============================================================
# QUBRIQ - CUMULATIVE AUTOMATED TEST RUNNER
# Stage 0 + Stage 1
# Windows PowerShell 5.1 compatible
# ============================================================

param(
    [string]$ApiUrl = "http://127.0.0.1:5080"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Frontend = Join-Path $Root "frontend"
$Backend = Join-Path $Root "backend\Qubriq.API"
$Logs = Join-Path $PSScriptRoot "logs"

New-Item -ItemType Directory -Path $Logs -Force | Out-Null

$Results = @()
$ApiProcess = $null
$PreviousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$env:ASPNETCORE_ENVIRONMENT = "Development"

function Add-Result {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Details
    )

    $Status = "FAIL"

    if ($Passed) {
        $Status = "PASS"
        Write-Host "[PASS] $Name : $Details" -ForegroundColor Green
    }
    else {
        Write-Host "[FAIL] $Name : $Details" -ForegroundColor Red
    }

    $script:Results += [PSCustomObject]@{
        Test = $Name
        Result = $Status
        Details = $Details
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "       QUBRIQ CUMULATIVE TESTS" -ForegroundColor Cyan
Write-Host "             STAGE 0 + 1" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

try {

    # TEST 1 - Project structure

    $Required = @(
        "$Root\README.md",
        "$Frontend\package.json",
        "$Frontend\package-lock.json",
        "$Frontend\index.html",
        "$Frontend\src\main.jsx",
        "$Frontend\src\App.jsx",
        "$Backend\Qubriq.API.csproj",
        "$Backend\Program.cs",
        "$Backend\Controllers\HealthController.cs",
        "$Backend\Data\QubriqDbContext.cs",
        "$Backend\Models\User.cs",
        "$Backend\Models\Role.cs",
        "$Backend\Models\UserRole.cs",
        "$Backend\Models\Customer.cs",
        "$Backend\Models\Shipment.cs",
        "$Backend\Models\Vehicle.cs",
        "$Backend\Models\Driver.cs",
        "$Backend\Models\ShipmentAssignment.cs",
        "$Backend\Migrations\20261123172349_InitialDatabaseSchema.cs",
        "$Backend\Migrations\20261123172349_InitialDatabaseSchema.Designer.cs",
        "$Backend\Migrations\QubriqDbContextModelSnapshot.cs",
        "$Root\docs\development\VERSIONS.txt",
        "$Root\docs\README.md",
        "$Root\docs\modules\STAGE-1-DATABASE-FOUNDATION.md"
    )

    $Missing = @($Required | Where-Object { -not (Test-Path $_) })

    Add-Result `
        "Project structure" `
        ($Missing.Count -eq 0) `
        $(if ($Missing.Count -eq 0) { "Required project, model, migration and documentation files exist." } else { "Missing: " + ($Missing -join "; ") })

    # TEST 2 - Frontend production build

    Push-Location $Frontend

    try {
        $Output = npm run build 2>&1
        $ExitCode = $LASTEXITCODE
        $Output | Out-File "$Logs\frontend_build.log"

        $BuildPassed = (
            $ExitCode -eq 0 -and
            (Test-Path "$Frontend\dist\index.html")
        )

        Add-Result "Frontend production build" $BuildPassed "Vite build exit code: $ExitCode"
    }
    catch {
        Add-Result "Frontend production build" $false $_.Exception.Message
    }
    finally {
        Pop-Location
    }

    # TEST 3 - Backend build

    Push-Location $Backend

    try {
        $Output = dotnet build --configuration Debug 2>&1
        $ExitCode = $LASTEXITCODE
        $Output | Out-File "$Logs\backend_build.log"

        Add-Result "Backend build" ($ExitCode -eq 0) "dotnet build exit code: $ExitCode"
    }
    catch {
        Add-Result "Backend build" $false $_.Exception.Message
    }
    finally {
        Pop-Location
    }

    # TEST 4 - API startup

    $ApiReady = $false
    $ApiPort = ([uri]$ApiUrl).Port

    $ExistingConnection = Get-NetTCPConnection `
        -LocalPort $ApiPort `
        -State Listen `
        -ErrorAction SilentlyContinue

    if ($ExistingConnection) {
        Add-Result "API startup" $false "Port $ApiPort is already occupied. Stop the existing API and retry."
    }
    else {
        try {
            $ApiProcess = Start-Process `
                -FilePath "dotnet" `
                -ArgumentList "run --no-build --urls `"$ApiUrl`"" `
                -WorkingDirectory $Backend `
                -PassThru `
                -WindowStyle Hidden `
                -RedirectStandardOutput "$Logs\api_stdout.log" `
                -RedirectStandardError "$Logs\api_stderr.log"

            $Deadline = (Get-Date).AddSeconds(45)

            while ((Get-Date) -lt $Deadline) {

                if ($ApiProcess.HasExited) {
                    break
                }

                try {
                    $Health = Invoke-RestMethod `
                        -Uri "$ApiUrl/api/health" `
                        -TimeoutSec 3

                    if ($Health.status -eq "Healthy") {
                        $ApiReady = $true
                        break
                    }
                }
                catch {
                    Start-Sleep -Seconds 1
                }
            }

            Add-Result "API startup" $ApiReady "API startup verification."
        }
        catch {
            Add-Result "API startup" $false $_.Exception.Message
        }
    }

    # TEST 5 - API health endpoint

    if ($ApiReady) {
        try {
            $Health = Invoke-RestMethod "$ApiUrl/api/health"

            $Valid = (
                $Health.status -eq "Healthy" -and
                $Health.application -eq "Qubriq API" -and
                $Health.stage -eq 1
            )

            Add-Result "API health endpoint" $Valid "Health response and Stage 1 validated."
        }
        catch {
            Add-Result "API health endpoint" $false $_.Exception.Message
        }
    }
    else {
        Add-Result "API health endpoint" $false "Skipped: API unavailable."
    }

    # TEST 6 - Root endpoint

    if ($ApiReady) {
        try {
            $RootResponse = Invoke-RestMethod "$ApiUrl/"

            $Valid = (
                $RootResponse.application -eq "Qubriq API" -and
                $RootResponse.status -eq "Running" -and
                $RootResponse.stage -eq 1
            )

            Add-Result "API root endpoint" $Valid "Root response and Stage 1 validated."
        }
        catch {
            Add-Result "API root endpoint" $false $_.Exception.Message
        }
    }
    else {
        Add-Result "API root endpoint" $false "Skipped: API unavailable."
    }

    # TEST 7 - OpenAPI

    if ($ApiReady) {
        try {
            $OpenApi = Invoke-RestMethod "$ApiUrl/openapi/v1.json"

            $Valid = $null -ne $OpenApi.openapi

            Add-Result "OpenAPI endpoint" $Valid "OpenAPI document verified."
        }
        catch {
            Add-Result "OpenAPI endpoint" $false $_.Exception.Message
        }
    }
    else {
        Add-Result "OpenAPI endpoint" $false "Skipped: API unavailable."
    }

    # TEST 8 - Database connectivity

    if ($ApiReady) {
        try {
            $Database = Invoke-RestMethod "$ApiUrl/api/database/health"

            $Valid = (
                $Database.database -eq "MySQL" -and
                $Database.status -eq "Connected" -and
                $Database.databaseName -eq "qubriq_dev"
            )

            Add-Result "MySQL database connectivity" $Valid "Database health endpoint validated."
        }
        catch {
            Add-Result "MySQL database connectivity" $false $_.Exception.Message
        }
    }
    else {
        Add-Result "MySQL database connectivity" $false "Skipped: API unavailable."
    }
}
finally {

    # CLEANUP

    if ($null -ne $ApiProcess) {
        if (-not $ApiProcess.HasExited) {
            Stop-Process -Id $ApiProcess.Id -Force -ErrorAction SilentlyContinue
        }
    }

    $env:ASPNETCORE_ENVIRONMENT = $PreviousEnvironment
}

# REPORT

$Passed = @($Results | Where-Object { $_.Result -eq "PASS" }).Count
$Failed = @($Results | Where-Object { $_.Result -eq "FAIL" }).Count

$Report = [PSCustomObject]@{
    Project = "Qubriq"
    Stage = 1
    Date = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    TotalTests = $Results.Count
    Passed = $Passed
    Failed = $Failed
    Tests = $Results
}

$Report | ConvertTo-Json -Depth 5 |
    Set-Content "$Logs\latest-test-report.json" -Encoding UTF8

$Results | Export-Csv "$Logs\latest-test-report.csv" -NoTypeInformation

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "             TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "Total  : $($Results.Count)"
Write-Host "Passed : $Passed" -ForegroundColor Green
Write-Host "Failed : $Failed" -ForegroundColor Red

Write-Host "`nReports: $Logs" -ForegroundColor Yellow

if ($Failed -gt 0) {
    Write-Host "`nSTAGE 1 FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "`nSTAGE 1 PASSED" -ForegroundColor Green
exit 0
