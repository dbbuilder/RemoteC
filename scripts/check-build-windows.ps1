# Check build status on Windows
# Diagnose build failures

param(
    [switch]$Verbose,
    [switch]$Restore
)

$ErrorActionPreference = "Stop"

Write-Host "RemoteC Build Diagnostics for Windows" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Change to solution directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Split-Path -Parent $scriptPath
Set-Location $solutionPath

Write-Host "Solution Path: $solutionPath" -ForegroundColor Gray
Write-Host ""

# Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = & dotnet --version
Write-Host "  .NET SDK Version: $dotnetVersion" -ForegroundColor Green

$runtimes = & dotnet --list-runtimes
Write-Host "  Installed Runtimes:" -ForegroundColor Gray
$runtimes | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
Write-Host ""

# Clean if requested
if ($Restore) {
    Write-Host "Cleaning solution..." -ForegroundColor Yellow
    & dotnet clean RemoteC.sln -v minimal
    Write-Host "  Clean completed" -ForegroundColor Green
    Write-Host ""
}

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
$restoreArgs = @("restore", "RemoteC.sln")
if ($Verbose) {
    $restoreArgs += "-v", "normal"
}

$restoreResult = & dotnet @restoreArgs 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  Restore failed!" -ForegroundColor Red
    Write-Host "  Error output:" -ForegroundColor Red
    $restoreResult | Where-Object { $_ -match "error" -or $_ -match "warning" } | ForEach-Object {
        Write-Host "    $_" -ForegroundColor Red
    }
} else {
    Write-Host "  Restore succeeded" -ForegroundColor Green
}
Write-Host ""

# Try building each project individually
Write-Host "Building projects individually..." -ForegroundColor Yellow

$projects = @(
    "src/RemoteC.Shared/RemoteC.Shared.csproj",
    "src/RemoteC.Data/RemoteC.Data.csproj",
    "src/RemoteC.Api/RemoteC.Api.csproj",
    "src/RemoteC.Web/RemoteC.Web.csproj",
    "src/RemoteC.Host/RemoteC.Host.csproj",
    "src/RemoteC.Client/RemoteC.Client.csproj"
)

$failedProjects = @()

foreach ($project in $projects) {
    if (Test-Path $project) {
        Write-Host "  Building $project..." -ForegroundColor Cyan
        
        $buildArgs = @("build", $project, "-c", "Release")
        if ($Verbose) {
            $buildArgs += "-v", "normal"
        } else {
            $buildArgs += "-v", "minimal"
        }
        
        $output = & dotnet @buildArgs 2>&1
        $exitCode = $LASTEXITCODE
        
        if ($exitCode -eq 0) {
            Write-Host "    ✓ Success" -ForegroundColor Green
        } else {
            Write-Host "    ✗ Failed" -ForegroundColor Red
            $failedProjects += $project
            
            # Show errors
            $errors = $output | Where-Object { $_ -match "error" }
            if ($errors) {
                Write-Host "    Errors:" -ForegroundColor Red
                $errors | Select-Object -First 5 | ForEach-Object {
                    Write-Host "      $_" -ForegroundColor Red
                }
            }
        }
    } else {
        Write-Host "  Project not found: $project" -ForegroundColor Yellow
    }
}

Write-Host ""

# Try building the full solution
Write-Host "Building full solution..." -ForegroundColor Yellow
$solutionBuildArgs = @("build", "RemoteC.sln", "-c", "Release")
if ($Verbose) {
    $solutionBuildArgs += "-v", "detailed"
} else {
    $solutionBuildArgs += "-v", "normal"
}

$solutionOutput = & dotnet @solutionBuildArgs 2>&1
$solutionExitCode = $LASTEXITCODE

if ($solutionExitCode -eq 0) {
    Write-Host "  ✓ Solution build succeeded!" -ForegroundColor Green
} else {
    Write-Host "  ✗ Solution build failed!" -ForegroundColor Red
    
    # Extract and display errors
    Write-Host "`nBuild Errors:" -ForegroundColor Red
    $solutionOutput | Where-Object { 
        $_ -match "error" -and 
        $_ -notmatch "0 Warning\(s\)" -and
        $_ -notmatch "Build FAILED"
    } | Select-Object -First 20 | ForEach-Object {
        Write-Host "  $_" -ForegroundColor Red
    }
    
    # Extract and display warnings
    $warnings = $solutionOutput | Where-Object { $_ -match "warning" }
    if ($warnings) {
        Write-Host "`nBuild Warnings:" -ForegroundColor Yellow
        $warnings | Select-Object -First 10 | ForEach-Object {
            Write-Host "  $_" -ForegroundColor Yellow
        }
    }
}

Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Diagnostics Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($failedProjects.Count -gt 0) {
    Write-Host "Failed Projects:" -ForegroundColor Red
    $failedProjects | ForEach-Object {
        Write-Host "  - $_" -ForegroundColor Red
    }
} else {
    Write-Host "All individual projects built successfully" -ForegroundColor Green
}

if ($solutionExitCode -eq 0) {
    Write-Host "Solution build: SUCCESS" -ForegroundColor Green
} else {
    Write-Host "Solution build: FAILED" -ForegroundColor Red
}

Write-Host ""
Write-Host "Recommendations:" -ForegroundColor Yellow
if ($solutionExitCode -ne 0) {
    Write-Host "  1. Run with -Restore flag to clean and restore packages" -ForegroundColor Gray
    Write-Host "  2. Run with -Verbose flag for detailed output" -ForegroundColor Gray
    Write-Host "  3. Check for missing dependencies or SDK components" -ForegroundColor Gray
    Write-Host "  4. Ensure all required workloads are installed:" -ForegroundColor Gray
    Write-Host "     dotnet workload list" -ForegroundColor Gray
}

Write-Host ""