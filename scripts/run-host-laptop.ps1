#!/usr/bin/env pwsh
# Quick script to run RemoteC Host on your laptop for testing
# Point it to your desktop's IP address

param(
    [Parameter(Mandatory=$true)]
    [string]$DesktopIP  # Your desktop's IP address where RemoteC is running
)

$ErrorActionPreference = "Stop"

# Script configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$hostPath = Join-Path $rootPath "src\RemoteC.Host"

Write-Host ""
Write-Host "RemoteC Host Quick Start" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

# Change to host directory
Set-Location $hostPath

# Check if already built
if (-not (Test-Path "bin\Debug\net8.0\RemoteC.Host.exe")) {
    Write-Host "Building RemoteC Host..." -ForegroundColor Yellow
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

# Create runtime configuration
Write-Host "Configuring connection to desktop at $DesktopIP..." -ForegroundColor Yellow

$config = @{
    ConnectionSettings = @{
        ServerUrl = "http://${DesktopIP}:7001"
        UseHttps = $false
        DeviceId = [System.Guid]::NewGuid().ToString()
        DeviceName = "$env:COMPUTERNAME-Laptop"
        Tags = @("laptop", "test", "dev")
    }
    EnableDevAuth = $true
    Logging = @{
        LogLevel = @{
            Default = "Information"
            "Microsoft.AspNetCore.SignalR.Client" = "Debug"
            "RemoteC.Host" = "Debug"
        }
    }
}

$configJson = $config | ConvertTo-Json -Depth 10
Set-Content -Path "appsettings.Development.json" -Value $configJson

# Display connection info
Write-Host ""
Write-Host "Starting RemoteC Host" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green
Write-Host ""
Write-Host "Connection Details:" -ForegroundColor Cyan
Write-Host "  Connecting to: http://${DesktopIP}:7001" -ForegroundColor White
Write-Host "  Device Name: $env:COMPUTERNAME-Laptop" -ForegroundColor White
Write-Host "  Device ID: (auto-generated)" -ForegroundColor White
Write-Host ""
Write-Host "Make sure on your desktop:" -ForegroundColor Yellow
Write-Host "  1. RemoteC is running (docker ps should show containers)" -ForegroundColor Gray
Write-Host "  2. You can access http://${DesktopIP}:7001/health" -ForegroundColor Gray
Write-Host "  3. Windows Firewall allows port 7001" -ForegroundColor Gray
Write-Host ""
Write-Host "Press Ctrl+C to stop the host" -ForegroundColor Gray
Write-Host ""

# Set environment variables
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:REMOTEC_SERVER_URL = "http://${DesktopIP}:7001"

# Run the host
dotnet run --no-build