# Fix missing test packages on Windows
# This script restores all missing test framework packages

$ErrorActionPreference = "Stop"

Write-Host "Fixing RemoteC Test Package References" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Change to solution directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Split-Path -Parent $scriptPath
Set-Location $solutionPath

# First, clean any stale references
Write-Host "Cleaning stale references..." -ForegroundColor Yellow

# Remove any obj and bin directories that might have stale references
$dirsToClean = @(
    "tests/RemoteC.Api.Tests/obj",
    "tests/RemoteC.Api.Tests/bin",
    "tests/RemoteC.Tests.Integration/obj",
    "tests/RemoteC.Tests.Integration/bin",
    "tests/RemoteC.Tests.Performance/obj",
    "tests/RemoteC.Tests.Performance/bin",
    "tests/RemoteC.Tests.Unit/obj",
    "tests/RemoteC.Tests.Unit/bin",
    "src/RemoteC.Host/obj",
    "src/RemoteC.Host/bin",
    "src/RemoteC.Client/obj",
    "src/RemoteC.Client/bin"
)

foreach ($dir in $dirsToClean) {
    if (Test-Path $dir) {
        Write-Host "  Removing $dir" -ForegroundColor Gray
        Remove-Item -Path $dir -Recurse -Force
    }
}

Write-Host ""

# Add missing packages to test projects
Write-Host "Adding missing test packages..." -ForegroundColor Yellow

# RemoteC.Api.Tests
Write-Host "  Fixing RemoteC.Api.Tests..." -ForegroundColor Cyan
Set-Location "tests/RemoteC.Api.Tests"
& dotnet add package xunit --version 2.6.6
& dotnet add package xunit.runner.visualstudio --version 2.5.6
& dotnet add package Moq --version 4.20.70
& dotnet add package FluentAssertions --version 6.12.0
& dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0

# RemoteC.Tests.Integration
Write-Host "`n  Fixing RemoteC.Tests.Integration..." -ForegroundColor Cyan
Set-Location "../RemoteC.Tests.Integration"
& dotnet add package xunit --version 2.6.6
& dotnet add package xunit.runner.visualstudio --version 2.5.6
& dotnet add package FluentAssertions --version 6.12.0
& dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0
& dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
& dotnet add package Testcontainers --version 3.6.0
& dotnet add package Testcontainers.MsSql --version 3.6.0
& dotnet add package Testcontainers.Redis --version 3.6.0
& dotnet add package Microsoft.AspNetCore.SignalR.Client --version 8.0.0

# RemoteC.Tests.Performance
Write-Host "`n  Fixing RemoteC.Tests.Performance..." -ForegroundColor Cyan
Set-Location "../RemoteC.Tests.Performance"
& dotnet add package BenchmarkDotNet --version 0.13.11
& dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0

# RemoteC.Tests.Unit (should already have packages, but let's ensure)
Write-Host "`n  Verifying RemoteC.Tests.Unit..." -ForegroundColor Cyan
Set-Location "../RemoteC.Tests.Unit"
& dotnet add package xunit --version 2.6.6
& dotnet add package xunit.runner.visualstudio --version 2.5.6
& dotnet add package Moq --version 4.20.70
& dotnet add package FluentAssertions --version 6.12.0
& dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0

Write-Host ""

# Return to solution root
Set-Location $solutionPath

# Clear NuGet cache for these packages to ensure fresh downloads
Write-Host "Clearing NuGet cache for test packages..." -ForegroundColor Yellow
& dotnet nuget locals all --clear

Write-Host ""

# Restore all packages
Write-Host "Restoring all packages..." -ForegroundColor Yellow
& dotnet restore RemoteC.sln --force

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPackage restore succeeded!" -ForegroundColor Green
} else {
    Write-Host "`nPackage restore failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Try building again
Write-Host "Attempting to build solution..." -ForegroundColor Yellow
& dotnet build RemoteC.sln -c Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBUILD SUCCEEDED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now run the ScreenCapture tests:" -ForegroundColor Green
    Write-Host "  dotnet test tests\RemoteC.Tests.Unit\RemoteC.Tests.Unit.csproj --filter FullyQualifiedName~ScreenCaptureServiceTests -c Release" -ForegroundColor Gray
} else {
    Write-Host "`nBuild still failing. Checking for remaining issues..." -ForegroundColor Red
    
    # Check if there are still FaithVision references
    Write-Host "`nChecking for cross-project references..." -ForegroundColor Yellow
    $faithVisionRefs = Get-ChildItem -Path . -Include *.csproj -Recurse | Select-String "FaithVision"
    if ($faithVisionRefs) {
        Write-Host "Found FaithVision references in:" -ForegroundColor Red
        $faithVisionRefs | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    }
}

Write-Host ""