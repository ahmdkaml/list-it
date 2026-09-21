param (
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("Core", "Shell", "UI", "App", "Tests", "Installer", "All")]
    [string]$Project,

    [string]$Configuration = "Release",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Building Project: $Project ($Configuration)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

switch ($Project) {
    "Core" {
        $projectPath = Join-Path $repoRoot "ListIt.Core/ListIt.Core.csproj"
        if ($Clean) { dotnet clean "$projectPath" -c $Configuration }
        dotnet build "$projectPath" -c $Configuration
    }
    "Shell" {
        $projectPath = Join-Path $repoRoot "ListIt.Shell/ListIt.Shell.csproj"
        if ($Clean) { dotnet clean "$projectPath" -c $Configuration }
        dotnet build "$projectPath" -c $Configuration
    }
    "UI" {
        $projectPath = Join-Path $repoRoot "ListIt.UI/ListIt.UI.csproj"
        if ($Clean) { dotnet clean "$projectPath" -c $Configuration }
        dotnet build "$projectPath" -c $Configuration
    }
    "Tests" {
        $projectPath = Join-Path $repoRoot "ListIt.Tests/ListIt.Tests.csproj"
        if ($Clean) { dotnet clean "$projectPath" -c $Configuration }
        dotnet test "$projectPath" -c $Configuration
    }
    "App" {
        & "$PSScriptRoot/build-app.ps1" -Configuration $Configuration -Clean:$Clean
    }
    "Installer" {
        & "$PSScriptRoot/build-installer.ps1" -Configuration $Configuration -Clean:$Clean
    }
    "All" {
        & "$PSScriptRoot/build-sln.ps1" -Configuration $Configuration
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build for $Project failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Host "`nProject $Project built successfully." -ForegroundColor Green
