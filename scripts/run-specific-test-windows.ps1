# Run specific tests on Windows
# Useful for debugging System.Drawing.Common issues

param(
    [Parameter(Mandatory=$true)]
    [string]$TestName,
    [string]$Project = "RemoteC.Tests.Unit",
    [switch]$Debug
)

$ErrorActionPreference = "Stop"

Write-Host "Running specific test: $TestName" -ForegroundColor Cyan
Write-Host "Project: $Project" -ForegroundColor Gray
Write-Host ""

# Change to solution directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Split-Path -Parent $scriptPath
Set-Location $solutionPath

# Build first
Write-Host "Building project..." -ForegroundColor Yellow
$buildConfig = if ($Debug) { "Debug" } else { "Release" }
dotnet build tests/$Project/$Project.csproj -c $buildConfig

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Run the specific test
Write-Host "`nRunning test..." -ForegroundColor Yellow
$testArgs = @(
    "test",
    "tests/$Project/$Project.csproj",
    "--filter", "FullyQualifiedName~$TestName",
    "--no-build",
    "-c", $buildConfig,
    "--logger", "console;verbosity=detailed"
)

if ($Debug) {
    Write-Host "Debug mode enabled - attach debugger now if needed" -ForegroundColor Yellow
    $testArgs += "--", "RunConfiguration.TestSessionTimeout=300000"
}

$result = & dotnet @testArgs
$exitCode = $LASTEXITCODE

# Display result
if ($exitCode -eq 0) {
    Write-Host "`n[OK] Test passed!" -ForegroundColor Green
} else {
    Write-Host "`n[FAIL] Test failed!" -ForegroundColor Red
    
    # Check for specific errors
    if ($result -match "System.PlatformNotSupportedException") {
        Write-Host "`nPlatform error detected - this should not happen on Windows!" -ForegroundColor Red
    }
    
    if ($result -match "System.Drawing") {
        Write-Host "`nSystem.Drawing error detected" -ForegroundColor Yellow
    }
}

exit $exitCode