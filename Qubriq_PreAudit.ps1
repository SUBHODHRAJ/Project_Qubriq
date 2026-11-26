
$ErrorActionPreference = "Stop"

$Root = "S:\Project_Qubriq"
$ReportDir = Join-Path $Root "_Audit"
New-Item -ItemType Directory -Path $ReportDir -Force | Out-Null

$Report = Join-Path $ReportDir "PreAudit_Inventory.txt"

Start-Transcript -Path $Report -Force

Write-Host "`n========== 1. GIT STATUS ==========" -ForegroundColor Cyan
Set-Location $Root
git status -sb
git log -5 --oneline

Write-Host "`n========== 2. BACKEND CONTROLLERS ==========" -ForegroundColor Cyan
$ControllerDir = Join-Path $Root "backend\Qubriq.API\Controllers"

Get-ChildItem $ControllerDir -Filter "*.cs" -File |
    Sort-Object Name |
    ForEach-Object {
        Write-Host "`n--- $($_.Name) ---" -ForegroundColor Yellow
        Select-String -Path $_.FullName -Pattern `
            '^\s*\[Route|^\s*\[Http(Get|Post|Put|Patch|Delete)|^\s*\[Authorize|^\s*public (async )?Task<IActionResult>|^\s*public IActionResult'
    }

Write-Host "`n========== 3. AUTH AND PROVISIONING SOURCE ==========" -ForegroundColor Cyan

$SearchDirs = @(
    (Join-Path $Root "backend\Qubriq.API\Controllers"),
    (Join-Path $Root "backend\Qubriq.API\DTOs"),
    (Join-Path $Root "backend\Qubriq.API\Services")
)

foreach ($Dir in $SearchDirs) {
    if (Test-Path $Dir) {
        Get-ChildItem $Dir -Filter "*.cs" -File -Recurse |
            Where-Object {
                $_.Name -match 'Auth|Login|AdminCustomer|AdminRecipient|RecipientShipment|Assignment|Request'
            } |
            ForEach-Object {
                Write-Host "`n--- $($_.FullName.Replace($Root,'')) ---" -ForegroundColor Yellow
                Select-String -Path $_.FullName -Pattern `
                    '^\s*\[Route|^\s*\[Http|^\s*public class|^\s*public sealed class|^\s*public .*{ get; set; }|Password|RecipientType|Role|IsActive' |
                    Select-Object -First 100
            }
    }
}

Write-Host "`n========== 4. FRONTEND ROUTES AND SERVICES ==========" -ForegroundColor Cyan

$Frontend = Join-Path $Root "frontend"

Get-ChildItem (Join-Path $Frontend "src") -Filter "*.jsx" -File -Recurse |
    Where-Object { $_.FullName -match 'routes|layouts' } |
    ForEach-Object {
        Write-Host "`n--- $($_.FullName.Replace($Root,'')) ---" -ForegroundColor Yellow
        Select-String -Path $_.FullName -Pattern `
            '<Route|path=|ProtectedRoute|roleHome|permission|module'
    }

Write-Host "`n========== 5. PROJECT FILES AND CONFIGURATION NAMES ==========" -ForegroundColor Cyan

Get-ChildItem $Root -Force |
    Select-Object Mode, Name, Length

Write-Host "`nBackend configuration filenames:"
Get-ChildItem (Join-Path $Root "backend\Qubriq.API") -Force -File |
    Where-Object { $_.Name -match 'appsettings|\.csproj|\.sln' } |
    Select-Object Name, Length

Write-Host "`nFrontend configuration filenames:"
Get-ChildItem $Frontend -Force -File |
    Select-Object Name, Length

Write-Host "`n========== 6. GENERATED FOLDER INVENTORY ==========" -ForegroundColor Cyan

$GeneratedNames = @("bin", "obj", "node_modules", "dist", ".vite")

Get-ChildItem $Root -Directory -Recurse -Force |
    Where-Object {
        $_.Name -in $GeneratedNames -and
        $_.FullName -notmatch '\\(\.git|_Backups|_Audit)\\'
    } |
    ForEach-Object {
        [PSCustomObject]@{
            Folder = $_.FullName
            SizeMB = [math]::Round(
                (Get-ChildItem $_.FullName -File -Recurse -Force -ErrorAction SilentlyContinue |
                    Measure-Object Length -Sum).Sum / 1MB, 2)
        }
    } |
    Format-Table -AutoSize

Write-Host "`n========== 7. SECRET-SAFE CONFIGURATION CHECK ==========" -ForegroundColor Cyan

$ConfigFiles = @(
    (Join-Path $Frontend "vite.config.js"),
    (Join-Path $Root "backend\Qubriq.API\Program.cs")
)

foreach ($File in $ConfigFiles) {
    if (Test-Path $File) {
        Write-Host "`n--- $File ---" -ForegroundColor Yellow
        Select-String -Path $File -Pattern `
            'UseMySql|UseMySQL|AddDbContext|AddAuthentication|AddAuthorization|MapControllers|Proxy|target:|localhost|127\.0\.0\.1' |
            ForEach-Object { $_.Line }
    }
}

Write-Host "`n========== PRE-AUDIT INVENTORY COMPLETE ==========" -ForegroundColor Green
Stop-Transcript

Write-Host "Report saved: $Report" -ForegroundColor Green
