#!/usr/bin/env pwsh
# Deploy RemoteC Host to a machine for testing
# This script builds and runs the host application

param(
    [string]$ServerUrl = "http://localhost:7001",  # Your desktop URL
    [string]$AccessCode = "",                      # Optional access code
    [switch]$AsService,                            # Install as Windows service
    [switch]$Console                               # Run in console mode (default)
)

$ErrorActionPreference = "Stop"

# Script configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$hostPath = Join-Path $rootPath "src\RemoteC.Host"

Write-Host ""
Write-Host "RemoteC Host Deployment" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan
Write-Host ""

# Check if running as admin (required for service installation)
if ($AsService) {
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    if (-not $isAdmin) {
        Write-Host "Administrator privileges required for service installation" -ForegroundColor Red
        Write-Host "Please run this script as Administrator" -ForegroundColor Yellow
        exit 1
    }
}

# Build the host
Write-Host "Building RemoteC Host..." -ForegroundColor Yellow
Set-Location $hostPath

# Build in Release mode
dotnet publish -c Release -r win-x64 --self-contained -o publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Create configuration
Write-Host ""
Write-Host "Creating configuration..." -ForegroundColor Yellow

$config = @{
    ConnectionSettings = @{
        ServerUrl = $ServerUrl
        UseHttps = $false
        AccessCode = $AccessCode
        DeviceId = [System.Guid]::NewGuid().ToString()
        DeviceName = $env:COMPUTERNAME
    }
    Logging = @{
        LogLevel = @{
            Default = "Information"
            "Microsoft.AspNetCore.SignalR" = "Debug"
            "RemoteC.Host" = "Debug"
        }
    }
}

$configJson = $config | ConvertTo-Json -Depth 10
Set-Content -Path "publish\appsettings.json" -Value $configJson

# Copy required files
Write-Host "Copying dependencies..." -ForegroundColor Yellow

# Check if Rust core library exists
$rustLibPath = Join-Path $rootPath "src\RemoteC.Core\target\release\remotec_core.dll"
if (Test-Path $rustLibPath) {
    Copy-Item $rustLibPath "publish\" -Force
    Write-Host "Copied Rust core library" -ForegroundColor Green
} else {
    Write-Host "Rust core library not found, using ControlR provider" -ForegroundColor Yellow
}

if ($AsService) {
    # Install as Windows Service
    Write-Host ""
    Write-Host "Installing as Windows Service..." -ForegroundColor Yellow
    
    # Stop existing service if running
    $service = Get-Service "RemoteCHost" -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "Stopping existing service..." -ForegroundColor Gray
        Stop-Service "RemoteCHost" -Force
        Start-Sleep -Seconds 2
        
        Write-Host "Removing existing service..." -ForegroundColor Gray
        sc.exe delete "RemoteCHost"
        Start-Sleep -Seconds 2
    }
    
    # Install new service
    $exePath = Join-Path $PWD "publish\RemoteC.Host.exe"
    New-Service -Name "RemoteCHost" `
                -DisplayName "RemoteC Host Service" `
                -Description "RemoteC remote control host service" `
                -BinaryPathName $exePath `
                -StartupType Automatic
    
    # Start the service
    Write-Host "Starting service..." -ForegroundColor Yellow
    Start-Service "RemoteCHost"
    
    Write-Host ""
    Write-Host "Service installed and started!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Service Status:" -ForegroundColor Cyan
    Get-Service "RemoteCHost" | Format-Table -AutoSize
    
} else {
    # Run in console mode
    Write-Host ""
    Write-Host "Starting RemoteC Host in console mode..." -ForegroundColor Yellow
    Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
    Write-Host ""
    
    Set-Location "publish"
    
    # Set environment variables for development
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:REMOTEC_SERVER_URL = $ServerUrl
    
    # Display connection info
    Write-Host "Connection Settings:" -ForegroundColor Cyan
    Write-Host "  Server URL: $ServerUrl" -ForegroundColor White
    Write-Host "  Device Name: $env:COMPUTERNAME" -ForegroundColor White
    if ($AccessCode) {
        Write-Host "  Access Code: $AccessCode" -ForegroundColor White
    }
    Write-Host ""
    
    # Run the host
    & ".\RemoteC.Host.exe"
}

Set-Location $rootPath