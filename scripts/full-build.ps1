param (
    [string]$Configuration = "Release",
    [switch]$Clean,
    [switch]$SkipRun,
    [switch]$Passive,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Magenta
Write-Host " Starting Full Build Pipeline (App + Installer)" -ForegroundColor Magenta
Write-Host "==================================================" -ForegroundColor Magenta

# 1. Build & Publish the App
& "$PSScriptRoot/build-app.ps1" -Configuration $Configuration -Clean:$Clean
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 2. Build the WiX Installer
& "$PSScriptRoot/build-installer.ps1" -Configuration $Configuration -Clean:$Clean
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 3. Run the Installer (unless -SkipRun is set)
if (-not $SkipRun) {
    Write-Host "`nLaunching the generated installer..." -ForegroundColor Cyan
    & "$PSScriptRoot/run-installer.ps1" -Passive:$Passive -Quiet:$Quiet
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Host "`n-SkipRun specified: Skipping installer execution." -ForegroundColor Yellow
}

Write-Host "`n==================================================" -ForegroundColor Green
Write-Host " Full Build Pipeline Finished Successfully!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
