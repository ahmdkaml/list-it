param (
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$solutionPath = Join-Path $repoRoot "ListIt.slnx"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Building Full Solution ($Configuration)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "Building $solutionPath..." -ForegroundColor Green
dotnet build "$solutionPath" -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Error "Solution build failed."
    exit $LASTEXITCODE
}

Write-Host "`nEntire solution built successfully (no components executed)." -ForegroundColor Green
