# Simple build check for Windows
param(
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "RemoteC Build Check" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host ""

# Change to solution directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Split-Path -Parent $scriptPath
Set-Location $solutionPath

Write-Host "Working directory: $solutionPath"
Write-Host ""

# Check .NET version
Write-Host "Checking .NET SDK..."
& dotnet --version
Write-Host ""

# Clean and restore
Write-Host "Cleaning solution..."
& dotnet clean RemoteC.sln -v minimal
Write-Host ""

Write-Host "Restoring packages..."
$restoreResult = & dotnet restore RemoteC.sln 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Restore failed!" -ForegroundColor Red
    $restoreResult | Select-Object -Last 20
    exit 1
}
Write-Host "Restore succeeded" -ForegroundColor Green
Write-Host ""

# Build solution
Write-Host "Building solution..."
if ($Verbose) {
    & dotnet build RemoteC.sln -c Release -v normal
} else {
    & dotnet build RemoteC.sln -c Release
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "BUILD SUCCEEDED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now run the tests:"
    Write-Host "  dotnet test tests\RemoteC.Tests.Unit\RemoteC.Tests.Unit.csproj --filter FullyQualifiedName~ScreenCaptureServiceTests -c Release"
} else {
    Write-Host ""
    Write-Host "BUILD FAILED!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Try running with -Verbose flag for more details"
}

exit $LASTEXITCODE