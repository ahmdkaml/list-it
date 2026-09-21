param ()

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Cleaning Build & Publish Artifacts" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$dirsToClean = @(
    (Join-Path $repoRoot "publish"),
    (Join-Path $repoRoot "dist"),
    (Join-Path $repoRoot "ListIt/bin"),
    (Join-Path $repoRoot "ListIt/obj"),
    (Join-Path $repoRoot "ListIt.Tests/bin"),
    (Join-Path $repoRoot "ListIt.Tests/obj"),
    (Join-Path $repoRoot "ListIt.Installer/bin"),
    (Join-Path $repoRoot "ListIt.Installer/obj")
)

foreach ($dir in $dirsToClean) {
    if (Test-Path $dir) {
        Write-Host "Removing: $dir" -ForegroundColor Yellow
        Remove-Item -Recurse -Force $dir -ErrorAction SilentlyContinue
    }
}

$filesToClean = @(
    (Join-Path $repoRoot "install.log")
)

foreach ($file in $filesToClean) {
    if (Test-Path $file) {
        Write-Host "Removing: $file" -ForegroundColor Yellow
        Remove-Item -Force $file -ErrorAction SilentlyContinue
    }
}

Write-Host "`nClean complete." -ForegroundColor Green
