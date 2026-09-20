param (
    [string]$Configuration = "Release",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$installerProject = Join-Path $repoRoot "ListIt.Installer/ListIt.Installer.csproj"
$distDir = Join-Path $repoRoot "dist"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Building ListIt Installer ($Configuration)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}

if ($Clean) {
    Write-Host "Cleaning dist directory..." -ForegroundColor Yellow
    Get-ChildItem -Path $distDir -Filter "*.msi" | Remove-Item -Force
}

Write-Host "Compiling WiX installer package..." -ForegroundColor Green
dotnet build "$installerProject" `
    -c $Configuration `
    -p:Platform=x64

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build ListIt installer."
    exit $LASTEXITCODE
}

# Locate generated MSI
$msiFiles = Get-ChildItem -Path (Join-Path $repoRoot "ListIt.Installer/bin") -Filter "ListIt.msi" -Recurse | Sort-Object LastWriteTime -Descending
if ($msiFiles.Count -gt 0) {
    $targetMsi = Join-Path $distDir "ListIt.msi"
    Copy-Item $msiFiles[0].FullName $targetMsi -Force
    Write-Host "`nInstaller successfully built and copied to:" -ForegroundColor Green
    Write-Host "  $targetMsi" -ForegroundColor White
} else {
    Write-Warning "Could not find generated ListIt.msi in bin directory."
}
