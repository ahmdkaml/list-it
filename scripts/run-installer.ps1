param (
    [string]$MsiPath = "",
    [switch]$Passive,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."

if ([string]::IsNullOrWhiteSpace($MsiPath)) {
    $defaultDist = Join-Path $repoRoot "dist/ListIt.msi"
    if (Test-Path $defaultDist) {
        $MsiPath = $defaultDist
    } else {
        $search = Get-ChildItem -Path (Join-Path $repoRoot "ListIt.Installer/bin") -Filter "ListIt.msi" -Recurse | Sort-Object LastWriteTime -Descending
        if ($search.Count -gt 0) {
            $MsiPath = $search[0].FullName
        }
    }
}

if (-not (Test-Path $MsiPath)) {
    Write-Error "Installer MSI not found. Please build the installer first using build-installer.ps1."
    exit 1
}

$logPath = Join-Path $repoRoot "install.log"
$argsList = @("/i", "`"$MsiPath`"", "/l*v", "`"$logPath`"")

if ($Passive) {
    $argsList += "/passive"
} elseif ($Quiet) {
    $argsList += "/quiet"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Launching ListIt Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MSI: $MsiPath" -ForegroundColor White
Write-Host "Log: $logPath" -ForegroundColor White

# Ensure no instances of ListIt are running before installation starts
Get-Process -Name ListIt -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

$proc = Start-Process -FilePath "msiexec.exe" -ArgumentList $argsList -Wait -PassThru

if ($proc.ExitCode -eq 0) {
    Write-Host "`nInstallation completed successfully." -ForegroundColor Green
} else {
    Write-Warning "`nmsiexec exited with code: $($proc.ExitCode). Check $logPath for details."
}

exit $proc.ExitCode
