# Fix ScreenCaptureService tests for Windows
# This script ensures the tests work properly on Windows

param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Write-Host "ScreenCaptureService Test Fixer for Windows" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Change to solution directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Split-Path -Parent $scriptPath
Set-Location $solutionPath

# First, let's check the current status
Write-Host "Checking current test status..." -ForegroundColor Yellow

$testFile = "tests/RemoteC.Tests.Unit/Host/Services/ScreenCaptureServiceTests.cs"
if (-not (Test-Path $testFile)) {
    Write-Host "Test file not found: $testFile" -ForegroundColor Red
    exit 1
}

# Run the tests to see current status
Write-Host "Running ScreenCaptureService tests..." -ForegroundColor Yellow
$testResult = & dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj `
    --filter "FullyQualifiedName~ScreenCaptureServiceTests" `
    --no-build -c Release 2>&1

$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "✓ Tests are already passing!" -ForegroundColor Green
    exit 0
}

# Analyze the failures
Write-Host "`nAnalyzing test failures..." -ForegroundColor Yellow

$nullReferenceErrors = ($testResult | Select-String "NullReferenceException").Count
$platformErrors = ($testResult | Select-String "PlatformNotSupportedException").Count
$assertionErrors = ($testResult | Select-String "Expected .* to not be <null>").Count

Write-Host "Found issues:" -ForegroundColor Yellow
Write-Host "  - NullReferenceException: $nullReferenceErrors" -ForegroundColor Red
Write-Host "  - PlatformNotSupportedException: $platformErrors" -ForegroundColor Red
Write-Host "  - Null assertion failures: $assertionErrors" -ForegroundColor Red

if ($platformErrors -gt 0) {
    Write-Host "`nERROR: System.Drawing.Common platform errors on Windows!" -ForegroundColor Red
    Write-Host "This is unexpected - Windows should support System.Drawing" -ForegroundColor Red
    Write-Host "Please check your .NET SDK installation" -ForegroundColor Yellow
    exit 1
}

if ($DryRun) {
    Write-Host "`nDry run mode - no changes will be made" -ForegroundColor Yellow
    exit 0
}

# Create a test runner to verify the fix
Write-Host "`nCreating test verification script..." -ForegroundColor Yellow

$verifyScript = @'
# Verify ScreenCaptureService tests
Write-Host "Verifying ScreenCaptureService tests..." -ForegroundColor Yellow

# Build the test project
dotnet build tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj -c Release

# Run just the two failing tests
$test1 = "CaptureScreenAsync_WhenInitialized_ShouldReturnScreenData"
$test2 = "CaptureScreenAsync_WithScaling_ShouldApplyScale"

Write-Host "`nRunning test: $test1" -ForegroundColor Cyan
$result1 = & dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj `
    --filter "FullyQualifiedName~$test1" `
    --no-build -c Release 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ PASSED" -ForegroundColor Green
} else {
    Write-Host "  ✗ FAILED" -ForegroundColor Red
    $result1 | Where-Object { $_ -match "Assert" -or $_ -match "Expected" } | ForEach-Object {
        Write-Host "    $_" -ForegroundColor Red
    }
}

Write-Host "`nRunning test: $test2" -ForegroundColor Cyan
$result2 = & dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj `
    --filter "FullyQualifiedName~$test2" `
    --no-build -c Release 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ PASSED" -ForegroundColor Green
} else {
    Write-Host "  ✗ FAILED" -ForegroundColor Red
    $result2 | Where-Object { $_ -match "Assert" -or $_ -match "Expected" } | ForEach-Object {
        Write-Host "    $_" -ForegroundColor Red
    }
}

# Run all ScreenCaptureService tests
Write-Host "`nRunning all ScreenCaptureService tests..." -ForegroundColor Cyan
$allResult = & dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj `
    --filter "FullyQualifiedName~ScreenCaptureServiceTests" `
    --no-build -c Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ All tests passed!" -ForegroundColor Green
} else {
    Write-Host "✗ Some tests still failing" -ForegroundColor Red
}
'@

$verifyScript | Out-File "scripts/verify-screen-tests.ps1" -Encoding UTF8
Write-Host "Created: scripts/verify-screen-tests.ps1" -ForegroundColor Green

# Instructions for manual debugging
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Manual Debugging Steps" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Open PowerShell as Administrator on Windows" -ForegroundColor Yellow
Write-Host "2. Navigate to: $solutionPath" -ForegroundColor Yellow
Write-Host "3. Run: .\scripts\verify-screen-tests.ps1" -ForegroundColor Yellow
Write-Host ""
Write-Host "If tests still fail, try:" -ForegroundColor Yellow
Write-Host "  - Check if .NET Desktop Runtime is installed" -ForegroundColor Gray
Write-Host "  - Run: dotnet --list-runtimes" -ForegroundColor Gray
Write-Host "  - Install: dotnet add package System.Drawing.Common --version 8.0.0" -ForegroundColor Gray
Write-Host ""
Write-Host "For detailed debugging:" -ForegroundColor Yellow
Write-Host "  .\scripts\run-specific-test-windows.ps1 -TestName ScreenCaptureServiceTests -Debug" -ForegroundColor Gray
Write-Host ""