#!/usr/bin/env pwsh
# Quick script to run RemoteC Host in development mode
# Uses the stub provider for easy testing without external dependencies

param(
    [string]$ServerUrl = "http://localhost:7001"
)

$ErrorActionPreference = "Stop"

# Script configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$hostPath = Join-Path $rootPath "src\RemoteC.Host"

Write-Host ""
Write-Host "RemoteC Host - Development Mode" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host ""

# Change to host directory
Set-Location $hostPath

# Check if RemoteC API is running
Write-Host "Checking if RemoteC API is accessible..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$ServerUrl/health" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ API is running at $ServerUrl" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ Cannot reach API at $ServerUrl" -ForegroundColor Red
    Write-Host "  Make sure RemoteC is running (docker ps)" -ForegroundColor Gray
    Write-Host "  Or run: .\scripts\fast-deploy.ps1" -ForegroundColor Gray
    Write-Host ""
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne 'y' -and $continue -ne 'Y') {
        exit 1
    }
}

# Set environment variables
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:REMOTEC_SERVER_URL = $ServerUrl

# Check provider configuration
$configPath = "appsettings.Development.json"
$providerType = "ControlR"
$rustLibPath = "remotec_core.dll"

if (Test-Path $configPath) {
    try {
        $config = Get-Content $configPath | ConvertFrom-Json
        $providerType = $config.RemoteControlProvider.Type
    } catch {
        # Ignore JSON parsing errors
    }
}

$rustAvailable = Test-Path $rustLibPath
$providerStatus = switch ($providerType) {
    "Rust" { if ($rustAvailable) { "Rust (Native)" } else { "Rust -> Stub (DLL not found)" } }
    "ControlR" { "ControlR (External)" }
    default { "Stub (Fallback)" }
}

# Display configuration
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Environment: Development" -ForegroundColor Gray
Write-Host "  Server URL: $ServerUrl" -ForegroundColor Gray
Write-Host "  Provider: $providerStatus" -ForegroundColor Gray
Write-Host "  Device ID: dev-host-001" -ForegroundColor Gray
if ($providerType -eq "Rust") {
    if ($rustAvailable) {
        $dllSize = (Get-Item $rustLibPath).Length
        Write-Host "  Rust Library: $rustLibPath ($([math]::Round($dllSize/1MB, 1)) MB)" -ForegroundColor Green
    } else {
        Write-Host "  Rust Library: Missing - run .\scripts\build-rust-core.ps1" -ForegroundColor Yellow
    }
}
Write-Host ""

# Ensure project is built
Write-Host "Ensuring project is built..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Package restore failed!" -ForegroundColor Red
    exit 1
}

dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Starting RemoteC Host..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

# Run the host
dotnet run --no-build