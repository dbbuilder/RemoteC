# RemoteC Windows Test Runner
# This script runs tests on Windows to avoid System.Drawing.Common platform issues

param(
    [string]$Project = "all",
    [string]$Configuration = "Release",
    [switch]$Verbose,
    [switch]$CollectCoverage
)

$ErrorActionPreference = "Stop"

Write-Host "RemoteC Windows Test Runner" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

# Change to solution directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Split-Path -Parent $scriptPath
Set-Location $solutionPath

Write-Host "Solution Path: $solutionPath" -ForegroundColor Gray
Write-Host ""

# Build the solution first
Write-Host "Building solution..." -ForegroundColor Yellow
$buildArgs = @("build", "RemoteC.sln", "-c", $Configuration)
if ($Verbose) {
    $buildArgs += "-v", "normal"
}

$buildResult = & dotnet @buildArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build succeeded!" -ForegroundColor Green
Write-Host ""

# Define test projects
$testProjects = @{
    "unit" = @(
        "tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj"
    )
    "api" = @(
        "tests/RemoteC.Api.Tests/RemoteC.Api.Tests.csproj"
    )
    "integration" = @(
        "tests/RemoteC.Tests.Integration/RemoteC.Tests.Integration.csproj"
    )
    "performance" = @(
        "tests/RemoteC.Tests.Performance/RemoteC.Tests.Performance.csproj"
    )
}

# Determine which projects to test
$projectsToTest = @()
if ($Project -eq "all") {
    $projectsToTest = $testProjects.Values | ForEach-Object { $_ }
} elseif ($testProjects.ContainsKey($Project)) {
    $projectsToTest = $testProjects[$Project]
} else {
    Write-Host "Invalid project: $Project" -ForegroundColor Red
    Write-Host "Valid options: all, unit, api, integration, performance"
    exit 1
}

# Flatten the array
$projectsToTest = $projectsToTest | ForEach-Object { $_ }

# Test results
$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0
$failedProjects = @()

# Run tests for each project
foreach ($testProject in $projectsToTest) {
    Write-Host "Running tests for: $testProject" -ForegroundColor Cyan
    Write-Host "----------------------------------------" -ForegroundColor Gray
    
    $testArgs = @("test", $testProject, "-c", $Configuration, "--no-build", "--logger", "console;verbosity=normal")
    
    if ($CollectCoverage) {
        $testArgs += "--collect:""XPlat Code Coverage"""
    }
    
    if ($Verbose) {
        $testArgs += "-v", "normal"
    }
    
    # Special handling for performance tests
    if ($testProject -like "*Performance*") {
        Write-Host "Note: Performance tests may take several minutes..." -ForegroundColor Yellow
        $testArgs += "--", "BenchmarkDotNet.Jobs.ShortRunJob"
    }
    
    # Run the tests
    $testOutput = & dotnet @testArgs 2>&1
    $exitCode = $LASTEXITCODE
    
    # Parse test results from output
    $testOutput | ForEach-Object {
        if ($_ -match "Total tests: (\d+)") {
            $projectTotal = [int]$Matches[1]
            $totalTests += $projectTotal
        }
        if ($_ -match "Passed: (\d+)") {
            $projectPassed = [int]$Matches[1]
            $passedTests += $projectPassed
        }
        if ($_ -match "Failed: (\d+)") {
            $projectFailed = [int]$Matches[1]
            $failedTests += $projectFailed
        }
        if ($_ -match "Skipped: (\d+)") {
            $projectSkipped = [int]$Matches[1]
            $skippedTests += $projectSkipped
        }
    }
    
    if ($exitCode -eq 0) {
        Write-Host "✓ Tests passed!" -ForegroundColor Green
    } else {
        Write-Host "✗ Tests failed!" -ForegroundColor Red
        $failedProjects += $testProject
        
        # Show failed test details
        Write-Host "`nFailed tests:" -ForegroundColor Red
        $testOutput | Where-Object { $_ -match "Failed" -and $_ -notmatch "Failed: \d+" } | ForEach-Object {
            Write-Host "  $_" -ForegroundColor Red
        }
    }
    
    Write-Host ""
}

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total Tests:   $totalTests" -ForegroundColor White
Write-Host "Passed:        $passedTests" -ForegroundColor Green
Write-Host "Failed:        $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Gray" })
Write-Host "Skipped:       $skippedTests" -ForegroundColor Yellow

if ($totalTests -gt 0) {
    $passRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
    Write-Host "Pass Rate:     $passRate%" -ForegroundColor $(if ($passRate -ge 95) { "Green" } elseif ($passRate -ge 80) { "Yellow" } else { "Red" })
}

Write-Host ""

if ($failedProjects.Count -gt 0) {
    Write-Host "Failed Projects:" -ForegroundColor Red
    $failedProjects | ForEach-Object {
        Write-Host "  - $_" -ForegroundColor Red
    }
    Write-Host ""
}

# Special check for System.Drawing tests
Write-Host "Checking for System.Drawing.Common issues..." -ForegroundColor Yellow
$drawingTests = @(
    "RemoteC.Tests.Unit.Host.Services.ScreenCaptureServiceTests"
)

$hasDrawingIssues = $false
foreach ($drawingTest in $drawingTests) {
    $testResult = & dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj --filter "FullyQualifiedName~$drawingTest" --no-build -c $Configuration 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ $drawingTest passed on Windows!" -ForegroundColor Green
    } else {
        Write-Host "✗ $drawingTest still failing on Windows" -ForegroundColor Red
        $hasDrawingIssues = $true
    }
}

if (-not $hasDrawingIssues) {
    Write-Host "`nAll System.Drawing.Common tests passed on Windows!" -ForegroundColor Green
}

Write-Host ""

# Exit with appropriate code
if ($failedTests -gt 0) {
    Write-Host "Test run completed with failures." -ForegroundColor Red
    exit 1
} else {
    Write-Host "All tests passed!" -ForegroundColor Green
    exit 0
}