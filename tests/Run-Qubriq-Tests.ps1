# ============================================================
# QUBRIQ - CUMULATIVE AUTOMATED TEST RUNNER
# Stage 0 + Stage 1 + Stage 2
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
Write-Host "             STAGE 0 + 1 + 2" -ForegroundColor Cyan
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


    # STAGE 2 - CUSTOMER AND BOOKING TESTS

    # TEST 9 - Stage 2 project structure

    $Stage2Required = @(
        "$Backend\Controllers\CustomersController.cs",
        "$Backend\Controllers\BookingsController.cs",
        "$Backend\DTOs\CreateCustomerRequest.cs",
        "$Backend\DTOs\UpdateCustomerRequest.cs",
        "$Backend\DTOs\UpdateCustomerStatusRequest.cs",
        "$Backend\DTOs\CreateBookingRequest.cs",
        "$Frontend\src\pages\CustomersPage.jsx",
        "$Frontend\src\pages\BookingsPage.jsx",
        "$Frontend\src\services\customerService.js",
        "$Frontend\src\services\bookingService.js",
        "$Frontend\src\styles\customers.css",
        "$Frontend\src\styles\bookings.css",
        "$Root\docs\modules\STAGE-2-CUSTOMER-MANAGEMENT.md",
        "$Root\docs\modules\STAGE-2-BOOKING-API.md"
    )

    $Stage2Missing = @(
        $Stage2Required | Where-Object { -not (Test-Path $_) }
    )

    Add-Result `
        "Stage 2 project structure" `
        ($Stage2Missing.Count -eq 0) `
        $(if ($Stage2Missing.Count -eq 0) { "Customer and booking files exist." } else { "Missing: " + ($Stage2Missing -join "; ") })

    if ($ApiReady) {

        # TEST 10 - Customer listing

        try {
            $Customers = @(Invoke-RestMethod "$ApiUrl/api/customers")

            $Valid = $true

            foreach ($Customer in $Customers) {
                if ($null -eq $Customer.id -or [string]::IsNullOrWhiteSpace([string]$Customer.companyName)) {
                    $Valid = $false
                    break
                }
            }

            Add-Result "Customer listing API" $Valid "HTTP request succeeded; returned $($Customers.Count) customer record(s)."
        }
        catch {
            Add-Result "Customer listing API" $false $_.Exception.Message
        }

        # TEST 11 - Active customer filter

        try {
            $ActiveCustomers = @(Invoke-RestMethod "$ApiUrl/api/customers?isActive=true")

            $Valid = $true

            foreach ($Customer in $ActiveCustomers) {
                if ($Customer.isActive -ne $true) {
                    $Valid = $false
                    break
                }
            }

            Add-Result "Active customer filter" $Valid "Returned $($ActiveCustomers.Count) active customer record(s)."
        }
        catch {
            Add-Result "Active customer filter" $false $_.Exception.Message
        }

        # TEST 12 - Booking listing

        try {
            $Bookings = @(Invoke-RestMethod "$ApiUrl/api/bookings")

            $Valid = $true

            foreach ($Booking in $Bookings) {
                if ($null -eq $Booking.id) {
                    $Valid = $false
                    break
                }
            }

            Add-Result "Booking listing API" $Valid "HTTP request succeeded; returned $($Bookings.Count) booking record(s)."
        }
        catch {
            Add-Result "Booking listing API" $false $_.Exception.Message
        }

        # TEST 13 - Booking status filter

        try {
            $FilteredBookings = @(
                Invoke-RestMethod "$ApiUrl/api/bookings?status=Confirmed"
            )

            $Valid = $true

            foreach ($Booking in $FilteredBookings) {
                if ([string]$Booking.status -ne "Confirmed") {
                    $Valid = $false
                    break
                }
            }

            Add-Result "Booking status filter" $Valid "Confirmed filter returned $($FilteredBookings.Count) matching booking record(s)."
        }
        catch {
            Add-Result "Booking status filter" $false $_.Exception.Message
        }

        # TEST 14 - Booking customer selection API

        try {
            $BookingCustomers = @(
                Invoke-RestMethod "$ApiUrl/api/bookings/customers"
            )

            $Valid = $true

            foreach ($Customer in $BookingCustomers) {
                if ($null -eq $Customer.id -or [string]::IsNullOrWhiteSpace([string]$Customer.companyName)) {
                    $Valid = $false
                    break
                }
            }

            Add-Result "Booking customer selection API" $Valid "Returned $($BookingCustomers.Count) customer option(s)."
        }
        catch {
            Add-Result "Booking customer selection API" $false $_.Exception.Message
        }

        # TEST 15 - Invalid customer submission must be rejected

        try {
            $Rejected = $false

            try {
                Invoke-RestMethod `
                    -Uri "$ApiUrl/api/customers" `
                    -Method Post `
                    -ContentType "application/json" `
                    -Body "{}" `
                    -ErrorAction Stop | Out-Null
            }
            catch {
                if ($null -ne $_.Exception.Response) {
                    $StatusCode = [int]$_.Exception.Response.StatusCode
                    $Rejected = ($StatusCode -eq 400)
                }
            }

            Add-Result "Invalid customer submission validation" $Rejected "Empty JSON submission expected HTTP 400."
        }
        catch {
            Add-Result "Invalid customer submission validation" $false $_.Exception.Message
        }

        # TEST 16 - Invalid booking submission must be rejected

        try {
            $Rejected = $false

            try {
                Invoke-RestMethod `
                    -Uri "$ApiUrl/api/bookings" `
                    -Method Post `
                    -ContentType "application/json" `
                    -Body "{}" `
                    -ErrorAction Stop | Out-Null
            }
            catch {
                if ($null -ne $_.Exception.Response) {
                    $StatusCode = [int]$_.Exception.Response.StatusCode
                    $Rejected = ($StatusCode -eq 400)
                }
            }

            Add-Result "Invalid booking submission validation" $Rejected "Empty JSON submission expected HTTP 400."
        }
        catch {
            Add-Result "Invalid booking submission validation" $false $_.Exception.Message
        }
    }
    else {
        Add-Result "Customer listing API" $false "Skipped: API unavailable."
        Add-Result "Active customer filter" $false "Skipped: API unavailable."
        Add-Result "Booking listing API" $false "Skipped: API unavailable."
        Add-Result "Booking status filter" $false "Skipped: API unavailable."
        Add-Result "Booking customer selection API" $false "Skipped: API unavailable."
        Add-Result "Invalid customer submission validation" $false "Skipped: API unavailable."
        Add-Result "Invalid booking submission validation" $false "Skipped: API unavailable."
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
    Stage = 2
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
    Write-Host "`nCUMULATIVE TESTS FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "`nCUMULATIVE TESTS PASSED" -ForegroundColor Green
exit 0
