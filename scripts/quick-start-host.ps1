# Quick Start Host - Simplified version
param(
    [Parameter(Mandatory=$true)]
    [string]$ServerIP
)

Write-Host "RemoteC Host Quick Start" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

$currentDir = Get-Location

# Build the Host if needed
if (-not (Test-Path "src\RemoteC.Host\bin\Release\net8.0\RemoteC.Host.exe")) {
    Write-Host "Building RemoteC Host..." -ForegroundColor Yellow
    dotnet build src\RemoteC.Host\RemoteC.Host.csproj -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed! Check for compilation errors." -ForegroundColor Red
        exit 1
    }
}

# Create config
Write-Host "Creating configuration..." -ForegroundColor Yellow
$config = @{
    ApiSettings = @{
        ApiUrl = "http://${ServerIP}:7001"
        SignalRUrl = "http://${ServerIP}:7002/hubs/session"
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

$config | Out-File -FilePath "src\RemoteC.Host\bin\Release\net8.0\appsettings.json" -Encoding UTF8

Write-Host ""
Write-Host "Starting RemoteC Host..." -ForegroundColor Green
Write-Host "Server: $ServerIP" -ForegroundColor White
Write-Host "Machine: $env:COMPUTERNAME" -ForegroundColor White
Write-Host ""

# Start the Host
Start-Process -FilePath "src\RemoteC.Host\bin\Release\net8.0\RemoteC.Host.exe" -WorkingDirectory "$currentDir\src\RemoteC.Host\bin\Release\net8.0"

Write-Host "RemoteC Host is starting..." -ForegroundColor Green
Write-Host "Look for the RemoteC icon in your system tray." -ForegroundColor Yellow
Write-Host ""
Write-Host "If PIN authentication is enabled, a PIN will be displayed." -ForegroundColor Yellow
Write-Host "Use this PIN when connecting from the web interface." -ForegroundColor Yellow