param (
    [string]$Project,
    [string]$Class,
    [string]$File,
    [string]$Test,
    [string]$Filter,
    [string]$Configuration = "Release",
    [switch]$List,
    [string]$Verbosity = "normal"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$solutionPath = Join-Path $repoRoot "ListIt.slnx"

# Resolve target project or default to solution
$targetPath = $solutionPath
if (-not [string]::IsNullOrWhiteSpace($Project)) {
    if ($Project -eq "Tests" -or $Project -eq "ListIt.Tests") {
        $targetPath = Join-Path $repoRoot "ListIt.Tests/ListIt.Tests.csproj"
    } elseif (Test-Path $Project) {
        $targetPath = (Resolve-Path $Project).Path
    } else {
        $found = Get-ChildItem -Path $repoRoot -Filter "*$Project*.csproj" -Recurse | Select-Object -First 1
        if ($found) {
            $targetPath = $found.FullName
        } else {
            Write-Error "Could not find project matching '$Project'."
            exit 1
        }
    }
}

# If -File was passed instead of -Class, extract class name
if (-not [string]::IsNullOrWhiteSpace($File) -and [string]::IsNullOrWhiteSpace($Class)) {
    $Class = [System.IO.Path]::GetFileNameWithoutExtension($File)
}

# Build filter expression
$filterExpr = ""
if (-not [string]::IsNullOrWhiteSpace($Filter)) {
    $filterExpr = $Filter
} elseif (-not [string]::IsNullOrWhiteSpace($Class) -and -not [string]::IsNullOrWhiteSpace($Test)) {
    $filterExpr = "FullyQualifiedName~$Class.$Test"
} elseif (-not [string]::IsNullOrWhiteSpace($Class)) {
    $filterExpr = "FullyQualifiedName~$Class"
} elseif (-not [string]::IsNullOrWhiteSpace($Test)) {
    $filterExpr = "FullyQualifiedName~$Test"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Running Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Target:        $targetPath" -ForegroundColor White
Write-Host "Configuration: $Configuration" -ForegroundColor White
if ($filterExpr) {
    Write-Host "Filter:        $filterExpr" -ForegroundColor Yellow
}
Write-Host ""

$argsList = @("test", $targetPath, "-c", $Configuration, "-v", $Verbosity)

if ($filterExpr) {
    $argsList += "--filter"
    $argsList += $filterExpr
}

if ($List) {
    $argsList += "--list-tests"
}

& dotnet @argsList

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nTest run completed successfully." -ForegroundColor Green
} else {
    Write-Warning "`nTest run failed with exit code: $LASTEXITCODE."
}

exit $LASTEXITCODE
