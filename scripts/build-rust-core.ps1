#!/usr/bin/env pwsh
# Build and install Rust core library for RemoteC Host

param(
    [switch]$Clean,
    [switch]$Release
)

$ErrorActionPreference = "Stop"

# Script configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$rustCorePath = Join-Path $rootPath "src\RemoteC.Core"
$hostPath = Join-Path $rootPath "src\RemoteC.Host"

Write-Host ""
Write-Host "RemoteC Rust Core Builder" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

# Check if Rust is installed
Write-Host "Checking Rust installation..." -ForegroundColor Yellow
try {
    $rustVersion = rustc --version
    Write-Host "✓ Found Rust: $rustVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Rust not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Rust from: https://rustup.rs/" -ForegroundColor Yellow
    Write-Host "Or run: winget install Rustlang.Rustup" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

try {
    $cargoVersion = cargo --version
    Write-Host "✓ Found Cargo: $cargoVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Cargo not found!" -ForegroundColor Red
    exit 1
}

# Check if Rust core directory exists
if (-not (Test-Path $rustCorePath)) {
    Write-Host "✗ Rust core directory not found: $rustCorePath" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Rust core directory found" -ForegroundColor Green

# Change to Rust core directory
Set-Location $rustCorePath

# Clean if requested
if ($Clean) {
    Write-Host ""
    Write-Host "Cleaning previous build..." -ForegroundColor Yellow
    cargo clean
    Write-Host "✓ Clean complete" -ForegroundColor Green
}

# Build the Rust library
Write-Host ""
Write-Host "Building Rust core library..." -ForegroundColor Yellow

$buildType = if ($Release) { "release" } else { "debug" }
$buildFlag = if ($Release) { "--release" } else { "" }

Write-Host "Build type: $buildType" -ForegroundColor Gray
Write-Host "This may take several minutes for the first build..." -ForegroundColor Gray
Write-Host ""

# Build with cargo and Windows features since this is a Windows host
if ($Release) {
    cargo build --release --features windows
} else {
    cargo build --features windows
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Rust build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Rust build successful!" -ForegroundColor Green

# Find the built library
$targetDir = if ($Release) { "target\release" } else { "target\debug" }
$libPath = Join-Path $rustCorePath $targetDir

# Look for the library file (could be .dll on Windows, .so on Linux, .dylib on macOS)
$libFiles = @(
    "remotec_core.dll",
    "libremotec_core.dll",
    "remotec-core.dll", 
    "libremotec-core.dll",
    "remotec_core.so",
    "libremotec_core.so",
    "remotec-core.so",
    "libremotec-core.so",
    "remotec_core.dylib",
    "libremotec_core.dylib",
    "remotec-core.dylib",
    "libremotec-core.dylib"
)

$foundLib = $null
foreach ($libFile in $libFiles) {
    $fullPath = Join-Path $libPath $libFile
    if (Test-Path $fullPath) {
        $foundLib = $fullPath
        break
    }
}

if (-not $foundLib) {
    Write-Host "✗ Could not find built library in $libPath" -ForegroundColor Red
    Write-Host "Expected files: $($libFiles -join ', ')" -ForegroundColor Gray
    exit 1
}

Write-Host "✓ Found library: $foundLib" -ForegroundColor Green

# Copy to host directories
Write-Host ""
Write-Host "Installing library to host..." -ForegroundColor Yellow

$hostTargets = @(
    "$hostPath\bin\Debug\net8.0\",
    "$hostPath\bin\Release\net8.0\",
    "$hostPath\bin\net8.0\win-x64\",
    "$hostPath\"
)

$copyCount = 0
foreach ($target in $hostTargets) {
    if (Test-Path $target) {
        $targetFile = Join-Path $target "remotec_core.dll"
        try {
            Copy-Item $foundLib $targetFile -Force
            Write-Host "  ✓ Copied to: $target" -ForegroundColor Green
            $copyCount++
        } catch {
            Write-Host "  ! Failed to copy to: $target" -ForegroundColor Yellow
        }
    } else {
        # Create directory if it doesn't exist
        try {
            New-Item -ItemType Directory -Path $target -Force | Out-Null
            $targetFile = Join-Path $target "remotec_core.dll"
            Copy-Item $foundLib $targetFile -Force
            Write-Host "  ✓ Created and copied to: $target" -ForegroundColor Green
            $copyCount++
        } catch {
            Write-Host "  ! Failed to create/copy to: $target" -ForegroundColor Yellow
        }
    }
}

if ($copyCount -eq 0) {
    Write-Host "✗ Failed to copy library to any host directory!" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Library installed to $copyCount locations" -ForegroundColor Green

# Verify the library can be loaded (basic check)
Write-Host ""
Write-Host "Verifying library..." -ForegroundColor Yellow

$testLib = Join-Path $hostPath "remotec_core.dll"
if (Test-Path $testLib) {
    try {
        # Try to load the DLL to check if it's valid
        $bytes = [System.IO.File]::ReadAllBytes($testLib)
        if ($bytes.Length -gt 0) {
            Write-Host "✓ Library file is valid ($($bytes.Length) bytes)" -ForegroundColor Green
        }
    } catch {
        Write-Host "! Library may have issues: $_" -ForegroundColor Yellow
    }
} else {
    Write-Host "! Library not found at expected location" -ForegroundColor Yellow
}

# Update host configuration to use Rust provider
Write-Host ""
Write-Host "Updating host configuration..." -ForegroundColor Yellow

$devConfigPath = Join-Path $hostPath "appsettings.Development.json"
if (Test-Path $devConfigPath) {
    try {
        $config = Get-Content $devConfigPath | ConvertFrom-Json
        $config.RemoteControlProvider.Type = "Rust"
        $config.RemoteControlProvider.Settings.FallbackToStub = $true
        
        $config | ConvertTo-Json -Depth 10 | Set-Content $devConfigPath
        Write-Host "✓ Updated configuration to use Rust provider" -ForegroundColor Green
    } catch {
        Write-Host "! Failed to update configuration: $_" -ForegroundColor Yellow
    }
}

# Return to original directory
Set-Location $rootPath

# Summary
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "Rust Core Build Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Library Details:" -ForegroundColor Cyan
Write-Host "  Source: $foundLib" -ForegroundColor White
Write-Host "  Build Type: $buildType" -ForegroundColor White
Write-Host "  Installed to: $copyCount host directories" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Run: .\scripts\run-host-dev.ps1" -ForegroundColor Gray
Write-Host "  2. Host will now use the Rust provider" -ForegroundColor Gray
Write-Host "  3. Check logs for 'Rust provider initialized'" -ForegroundColor Gray
Write-Host ""

if (-not $Release) {
    Write-Host "Note: Built in debug mode. For production, use: -Release" -ForegroundColor Yellow
}