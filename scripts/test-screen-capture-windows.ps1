# Simple script to test ScreenCapture on Windows
$ErrorActionPreference = "Stop"

Write-Host "Testing ScreenCapture on Windows" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

Set-Location "D:\dev2\remotec"

# Try building just what we need
Write-Host "Building test project..." -ForegroundColor Yellow
Set-Location "tests\RemoteC.Tests.Unit"

# Build the test project
& dotnet build -c Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build succeeded!" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Running ScreenCapture tests..." -ForegroundColor Yellow
    # Run the specific tests
    & dotnet test --filter "FullyQualifiedName~ScreenCaptureServiceTests" -c Release --no-build -v normal
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nALL TESTS PASSED!" -ForegroundColor Green
        Write-Host "The ScreenCapture tests work correctly on Windows." -ForegroundColor Green
    } else {
        Write-Host "`nTests failed. Check the output above for details." -ForegroundColor Red
    }
} else {
    Write-Host "Build failed. Trying to fix dependencies..." -ForegroundColor Red
    
    # Add missing packages if needed
    Write-Host "`nAdding test packages..." -ForegroundColor Yellow
    & dotnet add package xunit --version 2.6.6
    & dotnet add package xunit.runner.visualstudio --version 2.5.6  
    & dotnet add package Moq --version 4.20.70
    & dotnet add package FluentAssertions --version 6.12.0
    & dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0
    
    # Try building again
    Write-Host "`nRetrying build..." -ForegroundColor Yellow
    & dotnet build -c Release
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build succeeded after adding packages!" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "Running ScreenCapture tests..." -ForegroundColor Yellow
        & dotnet test --filter "FullyQualifiedName~ScreenCaptureServiceTests" -c Release --no-build -v normal
    }
}

# Return to original directory
Set-Location "D:\dev2\remotec"