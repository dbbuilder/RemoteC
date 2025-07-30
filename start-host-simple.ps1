# RemoteC Host - Simple Direct Startup
param(
    [string]$ServerIP = "192.168.1.100",
    [int]$ServerPort = 17001
)

Write-Host "RemoteC Host Simple Startup" -ForegroundColor Cyan
Write-Host "===========================" -ForegroundColor Cyan
Write-Host ""

# Get the script directory and navigate to the host executable
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$hostExePath = Join-Path $scriptDir "src\RemoteC.Host\bin\Release\net8.0\win-x64\RemoteC.Host.exe"

# Check if the executable exists
if (-not (Test-Path $hostExePath)) {
    Write-Host "ERROR: Host executable not found at: $hostExePath" -ForegroundColor Red
    Write-Host "Please run 'dotnet build src/RemoteC.Host/RemoteC.Host.csproj -c Release' first" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Server IP: $ServerIP" -ForegroundColor Green
Write-Host "Server Port: $ServerPort" -ForegroundColor Green
Write-Host "Machine: $env:COMPUTERNAME" -ForegroundColor Green
Write-Host ""

# Create config directory
$configDir = Split-Path -Parent $hostExePath
$configPath = Join-Path $configDir "appsettings.json"

# Create a basic configuration
$config = @{
    ApiSettings = @{
        ApiUrl = "http://${ServerIP}:${ServerPort}"
        SignalRUrl = "http://${ServerIP}:17002/hubs/session"
    }
    HostSettings = @{
        MachineName = $env:COMPUTERNAME
        AutoStart = $true
        EnablePinAuthentication = $true
    }
    Logging = @{
        LogLevel = @{
            Default = "Information"
        }
    }
} | ConvertTo-Json -Depth 10

$config | Out-File -FilePath $configPath -Encoding UTF8 -Force

Write-Host "Configuration created at: $configPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "Starting RemoteC Host..." -ForegroundColor Green

# Start the host executable
try {
    Set-Location -Path $configDir
    & $hostExePath
} catch {
    Write-Host "ERROR: Failed to start host: $_" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}