param (
    [string]$Configuration = "Release",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$projectPath = Join-Path $repoRoot "ListIt/ListIt.csproj"
$publishDir = Join-Path $repoRoot "publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Building ListIt Application ($Configuration)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($Clean) {
    Write-Host "Cleaning output directory: $publishDir" -ForegroundColor Yellow
    if (Test-Path $publishDir) {
        Remove-Item -Recurse -Force $publishDir
    }
}

Write-Host "Publishing standalone single-file binary..." -ForegroundColor Green
dotnet publish "$projectPath" `
    -c $Configuration `
    -r win-x64 `
    --self-contained `
    -o "$publishDir"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build ListIt application."
    exit $LASTEXITCODE
}

Write-Host "`nApp successfully built and published to:" -ForegroundColor Green
Write-Host "  $(Join-Path $publishDir 'ListIt.exe')" -ForegroundColor White
