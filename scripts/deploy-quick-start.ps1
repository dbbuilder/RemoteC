# RemoteC Quick Start Deployment Script
# This script sets up RemoteC for testing between two machines

param(
    [Parameter(Mandatory=$false)]
    [string]$Mode = "Server", # Server or Host
    
    [Parameter(Mandatory=$false)]
    [string]$ServerIP = "localhost",
    
    [Parameter(Mandatory=$false)]
    [string]$MachineName = $env:COMPUTERNAME
)

$ErrorActionPreference = "Stop"

Write-Host "RemoteC Quick Start Deployment" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    Write-Host "This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Please restart PowerShell as Administrator and try again." -ForegroundColor Yellow
    exit 1
}

function Deploy-Server {
    Write-Host "Deploying RemoteC Server Components..." -ForegroundColor Yellow
    
    # Check if Docker is installed
    $dockerInstalled = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerInstalled) {
        Write-Host "Docker is not installed. Please install Docker Desktop first." -ForegroundColor Red
        Write-Host "Download from: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
        exit 1
    }
    
    # Create deployment directory
    $deployDir = "C:\RemoteC-Server"
    if (-not (Test-Path $deployDir)) {
        New-Item -ItemType Directory -Path $deployDir -Force | Out-Null
    }
    
    # Create docker-compose file
    $dockerCompose = @"
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2019-latest
    container_name: remotec-sqlserver
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=RemoteC@2024!Strong
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    container_name: remotec-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    restart: unless-stopped

  api:
    image: mcr.microsoft.com/dotnet/aspnet:8.0
    container_name: remotec-api
    working_dir: /app
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:7001;http://+:7002
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=RemoteC2Db;User Id=sa;Password=RemoteC@2024!Strong;TrustServerCertificate=true
      - ConnectionStrings__Redis=redis:6379
      - Logging__LogLevel__Default=Information
    ports:
      - "7001:7001"
      - "7002:7002"
    depends_on:
      - sqlserver
      - redis
    volumes:
      - ./api:/app
    command: ["dotnet", "RemoteC.Api.dll"]
    restart: unless-stopped

volumes:
  sqlserver-data:
  redis-data:
"@

    $dockerCompose | Out-File -FilePath "$deployDir\docker-compose.yml" -Encoding UTF8
    
    # Create startup script
    $startupScript = @"
@echo off
echo Starting RemoteC Server...
cd /d C:\RemoteC-Server
docker-compose up -d
echo.
echo RemoteC Server is starting...
echo.
echo API will be available at:
echo   http://localhost:7001 (API)
echo   http://localhost:7002 (SignalR)
echo.
echo To check status: docker-compose ps
echo To view logs: docker-compose logs -f
echo To stop: docker-compose down
pause
"@
    
    $startupScript | Out-File -FilePath "$deployDir\start-server.bat" -Encoding ASCII
    
    # Build and publish API
    Write-Host "Building API..." -ForegroundColor Yellow
    $currentDir = Get-Location
    
    if (Test-Path "$currentDir\src\RemoteC.Api\RemoteC.Api.csproj") {
        dotnet publish "$currentDir\src\RemoteC.Api\RemoteC.Api.csproj" -c Release -o "$deployDir\api"
        
        # Create basic appsettings
        $appSettings = @{
            Logging = @{
                LogLevel = @{
                    Default = "Information"
                    Microsoft = "Warning"
                }
            }
            AllowedHosts = "*"
            Authentication = @{
                Enabled = $false  # Disable for testing
            }
        } | ConvertTo-Json -Depth 10
        
        $appSettings | Out-File -FilePath "$deployDir\api\appsettings.Development.json" -Encoding UTF8
    }
    
    # Open firewall ports
    Write-Host "Configuring Windows Firewall..." -ForegroundColor Yellow
    netsh advfirewall firewall add rule name="RemoteC API" dir=in action=allow protocol=TCP localport=7001 | Out-Null
    netsh advfirewall firewall add rule name="RemoteC SignalR" dir=in action=allow protocol=TCP localport=7002 | Out-Null
    netsh advfirewall firewall add rule name="SQL Server" dir=in action=allow protocol=TCP localport=1433 | Out-Null
    netsh advfirewall firewall add rule name="Redis" dir=in action=allow protocol=TCP localport=6379 | Out-Null
    
    Write-Host ""
    Write-Host "Server deployment complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "To start the server:" -ForegroundColor Yellow
    Write-Host "  cd $deployDir" -ForegroundColor White
    Write-Host "  .\start-server.bat" -ForegroundColor White
    Write-Host ""
    Write-Host "Your server IP addresses:" -ForegroundColor Yellow
    Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" } | ForEach-Object {
        Write-Host "  $($_.IPAddress)" -ForegroundColor White
    }
}

function Deploy-Host {
    param([string]$ServerIP)
    
    Write-Host "Deploying RemoteC Host Component..." -ForegroundColor Yellow
    Write-Host "Connecting to server at: $ServerIP" -ForegroundColor Cyan
    
    # Create deployment directory
    $deployDir = "C:\RemoteC-Host"
    if (-not (Test-Path $deployDir)) {
        New-Item -ItemType Directory -Path $deployDir -Force | Out-Null
    }
    
    # Build and publish Host
    Write-Host "Building Host application..." -ForegroundColor Yellow
    $currentDir = Get-Location
    
    if (Test-Path "$currentDir\src\RemoteC.Host\RemoteC.Host.csproj") {
        dotnet publish "$currentDir\src\RemoteC.Host\RemoteC.Host.csproj" -c Release -o "$deployDir"
        
        # Create configuration
        $hostConfig = @{
            ApiSettings = @{
                ApiUrl = "http://${ServerIP}:7001"
                SignalRUrl = "http://${ServerIP}:7002/hubs/session"
            }
            HostSettings = @{
                MachineName = $MachineName
                AutoStart = $true
                EnablePinAuthentication = $true
                ScreenCaptureQuality = 85
            }
            Logging = @{
                LogLevel = @{
                    Default = "Information"
                }
            }
        } | ConvertTo-Json -Depth 10
        
        $hostConfig | Out-File -FilePath "$deployDir\appsettings.json" -Encoding UTF8
    }
    
    # Create startup script
    $startupScript = @"
@echo off
echo Starting RemoteC Host...
cd /d C:\RemoteC-Host
start RemoteC.Host.exe
echo.
echo RemoteC Host is running.
echo Connected to server: $ServerIP
echo Machine name: $MachineName
echo.
echo The host will appear in the RemoteC dashboard once connected.
echo Check the system tray for the RemoteC icon.
pause
"@
    
    $startupScript | Out-File -FilePath "$deployDir\start-host.bat" -Encoding ASCII
    
    # Create Windows service (optional)
    $createService = Read-Host "Install as Windows Service? (Y/N)"
    if ($createService -eq "Y") {
        Write-Host "Installing Windows Service..." -ForegroundColor Yellow
        sc.exe create "RemoteCHost" binPath="$deployDir\RemoteC.Host.exe" start=auto DisplayName="RemoteC Host Service"
        sc.exe description "RemoteCHost" "RemoteC remote access host service"
    }
    
    Write-Host ""
    Write-Host "Host deployment complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "To start the host:" -ForegroundColor Yellow
    Write-Host "  cd $deployDir" -ForegroundColor White
    Write-Host "  .\start-host.bat" -ForegroundColor White
    Write-Host ""
    Write-Host "Or start the service:" -ForegroundColor Yellow
    Write-Host "  sc.exe start RemoteCHost" -ForegroundColor White
}

# Main execution
switch ($Mode) {
    "Server" {
        Deploy-Server
    }
    "Host" {
        if ($ServerIP -eq "localhost") {
            $ServerIP = Read-Host "Enter the RemoteC Server IP address"
        }
        Deploy-Host -ServerIP $ServerIP
    }
    default {
        Write-Host "Invalid mode. Use 'Server' or 'Host'" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host ""

# Show next steps
if ($Mode -eq "Server") {
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Start the server using the start-server.bat script" -ForegroundColor White
    Write-Host "2. Deploy the Host component on the machine(s) you want to control" -ForegroundColor White
    Write-Host "3. Access the web interface at http://localhost:7001" -ForegroundColor White
} else {
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Start the host using the start-host.bat script" -ForegroundColor White
    Write-Host "2. The host will connect to the server at $ServerIP" -ForegroundColor White
    Write-Host "3. Access the web interface at http://${ServerIP}:7001 to control this machine" -ForegroundColor White
}