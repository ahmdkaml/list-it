param (
    [switch]$Build,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$publishExe = Join-Path $repoRoot "publish/ListIt.exe"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Running List-it Application" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($Build -or -not (Test-Path $publishExe)) {
    Write-Host "Building application first..." -ForegroundColor Yellow
    & "$PSScriptRoot/build-app.ps1" -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "Launching: $publishExe" -ForegroundColor Green
$proc = Start-Process -FilePath $publishExe -PassThru

Write-Host "`nList-it started successfully with PID: $($proc.Id)" -ForegroundColor Green
