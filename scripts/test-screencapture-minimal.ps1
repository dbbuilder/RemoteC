# Minimal script to test ScreenCapture on Windows
Write-Host "Running ScreenCapture Tests on Windows (Minimal Approach)" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan

Set-Location "D:\dev2\remotec"

# Clean obj/bin directories first
Write-Host "`nCleaning build artifacts..." -ForegroundColor Yellow
Remove-Item -Path "tests\RemoteC.Tests.Unit\obj" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "tests\RemoteC.Tests.Unit\bin" -Recurse -Force -ErrorAction SilentlyContinue

# Go directly to test project
Set-Location "tests\RemoteC.Tests.Unit"

# Restore packages
Write-Host "`nRestoring packages..." -ForegroundColor Yellow
dotnet restore

# Build
Write-Host "`nBuilding test project..." -ForegroundColor Yellow
dotnet build -c Release

# Run the tests
Write-Host "`nRunning ScreenCapture tests..." -ForegroundColor Cyan
dotnet test --filter "FullyQualifiedName~ScreenCaptureServiceTests" -c Release --no-build

Write-Host "`nDone!" -ForegroundColor Green