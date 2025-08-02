# RemoteC Demo Deployment Script for Windows
# One-click deployment for testing across networks

param(
    [Parameter(Position=0)]
    [ValidateSet('start', 'stop', 'clean', 'status', 'logs')]
    [string]$Action = 'start'
)

$ErrorActionPreference = "Stop"

# Script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

# Project name for docker-compose
$ProjectName = "remotec-demo"

# Colors
function Write-Info { Write-Host "[INFO] $args" -ForegroundColor Blue }
function Write-Success { Write-Host "[SUCCESS] $args" -ForegroundColor Green }
function Write-Warning { Write-Host "[WARNING] $args" -ForegroundColor Yellow }
function Write-Error { Write-Host "[ERROR] $args" -ForegroundColor Red }

Write-Host "RemoteC Demo Deployment System" -ForegroundColor Blue
Write-Host "=============================="

# Check prerequisites
function Check-Prerequisites {
    Write-Info "Checking prerequisites..."
    
    # Check Docker
    try {
        $dockerVersion = docker --version 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Docker command failed"
        }
        Write-Info "Found: $dockerVersion"
    } catch {
        Write-Error "Docker is not installed or not in PATH. Please install Docker Desktop for Windows."
        Write-Info "Download from: https://www.docker.com/products/docker-desktop/"
        exit 1
    }
    
    # Check Docker Compose
    try {
        $composeVersion = docker-compose --version 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Docker Compose command failed"
        }
        Write-Info "Found: $composeVersion"
    } catch {
        Write-Error "Docker Compose is not installed. Please install Docker Compose."
        exit 1
    }
    
    # Check if Docker is running - with better error handling
    Write-Info "Checking if Docker daemon is running..."
    $retries = 3
    $success = $false
    
    for ($i = 1; $i -le $retries; $i++) {
        try {
            # Try different methods to check Docker status
            $dockerInfo = docker version --format '{{.Server.Version}}' 2>&1
            if ($LASTEXITCODE -eq 0) {
                $success = $true
                Write-Info "Docker daemon is running (version: $dockerInfo)"
                break
            }
        } catch {}
        
        if ($i -lt $retries) {
            Write-Warning "Docker check failed, retrying in 2 seconds... (attempt $i/$retries)"
            Start-Sleep -Seconds 2
        }
    }
    
    if (-not $success) {
        Write-Error "Docker is not running or not accessible."
        Write-Info "Please ensure:"
        Write-Info "  1. Docker Desktop is running (check system tray)"
        Write-Info "  2. Docker Desktop is set to 'Windows containers' or 'Linux containers'"
        Write-Info "  3. You may need to restart Docker Desktop"
        Write-Info ""
        Write-Info "If Docker Desktop is running, try:"
        Write-Info "  - Right-click Docker Desktop tray icon → Restart"
        Write-Info "  - Run this script as Administrator"
        Write-Info "  - Check Windows Defender Firewall settings"
        exit 1
    }
    
    Write-Success "All prerequisites met!"
}

# Get machine IP address
function Get-MachineIP {
    $ip = ""
    
    # Get primary network adapter IP
    $adapters = Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
        $_.InterfaceAlias -notlike "*Loopback*" -and 
        $_.InterfaceAlias -notlike "*vEthernet*" -and
        $_.IPAddress -notlike "169.254.*"
    }
    
    if ($adapters) {
        $ip = $adapters[0].IPAddress
    }
    
    # Fallback to localhost
    if (-not $ip) {
        $ip = "localhost"
    }
    
    return $ip
}

# Setup environment
function Setup-Environment {
    Write-Info "Setting up environment..."
    
    Set-Location $RootDir
    
    # Check if .env exists
    if (-not (Test-Path ".env")) {
        Write-Info "Creating .env file from template..."
        Copy-Item ".env.template" ".env"
        
        # Get machine IP
        $machineIP = Get-MachineIP
        Write-Info "Detected machine IP: $machineIP"
        
        # Update .env with machine IP
        $envContent = Get-Content ".env"
        $envContent = $envContent -replace "HOST_IP=.*", "HOST_IP=$machineIP"
        $envContent = $envContent -replace "REACT_APP_API_URL=.*", "REACT_APP_API_URL=http://${machineIP}:7001"
        $envContent = $envContent -replace "REACT_APP_HUB_URL=.*", "REACT_APP_HUB_URL=http://${machineIP}:7001/hubs"
        
        # Generate JWT secret
        $jwtSecret = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object {[char]$_})
        $envContent = $envContent -replace "JWT_SECRET=.*", "JWT_SECRET=$jwtSecret"
        
        Set-Content ".env" $envContent
        
        Write-Success ".env file created and configured!"
    } else {
        Write-Warning ".env file already exists. Using existing configuration."
        $machineIP = (Get-Content ".env" | Select-String "HOST_IP=").Line.Split('=')[1]
        Write-Info "Using IP from .env: $machineIP"
    }
    
    return $machineIP
}

# Build images
function Build-Images {
    Write-Info "Building Docker images..."
    
    # Save current error action preference
    $oldErrorAction = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"
    
    # Run docker-compose build
    docker-compose -p $ProjectName -f docker-compose.demo.yml build --parallel
    $buildResult = $LASTEXITCODE
    
    # Restore error action preference
    $ErrorActionPreference = $oldErrorAction
    
    # Check if build was successful
    if ($buildResult -ne 0) {
        Write-Error "Docker build failed!"
        exit 1
    }
    
    Write-Success "Docker images built successfully!"
}

# Start services
function Start-Services {
    Write-Info "Starting services..."
    
    # Stop any running services first
    Write-Info "Stopping any existing services..."
    $ErrorActionPreference = "SilentlyContinue"
    docker-compose -p $ProjectName -f docker-compose.demo.yml down
    $ErrorActionPreference = "Stop"
    
    # Start services
    Write-Info "Starting new services..."
    $ErrorActionPreference = "SilentlyContinue"
    $output = & docker-compose -p $ProjectName -f docker-compose.demo.yml up -d 2>&1
    $startResult = $LASTEXITCODE
    $ErrorActionPreference = "Stop"
    
    # Display output if there was any
    if ($output) {
        $output | ForEach-Object { Write-Host $_ }
    }
    
    if ($startResult -ne 0) {
        Write-Error "Failed to start services!"
        exit 1
    }
    
    Write-Info "Waiting for services to be ready..."
    
    # Wait for database
    Write-Info "Waiting for SQL Server..."
    for ($i = 1; $i -le 30; $i++) {
        try {
            docker-compose -p $ProjectName -f docker-compose.demo.yml exec -T db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "SELECT 1" 2>&1 | Out-Null
            Write-Success "SQL Server is ready!"
            break
        } catch {
            Write-Host "." -NoNewline
            Start-Sleep -Seconds 2
        }
    }
    Write-Host ""
    
    # Initialize database
    Write-Info "Initializing database..."
    docker-compose -p $ProjectName -f docker-compose.demo.yml exec -T db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RemoteC2Db') BEGIN CREATE DATABASE RemoteC2Db; END"
    
    # Wait for API
    Write-Info "Waiting for API..."
    for ($i = 1; $i -le 30; $i++) {
        try {
            Invoke-RestMethod -Uri "http://localhost:7001/health" -Method Get | Out-Null
            Write-Success "API is ready!"
            break
        } catch {
            Write-Host "." -NoNewline
            Start-Sleep -Seconds 2
        }
    }
    Write-Host ""
    
    # Wait for Web
    Write-Info "Waiting for Web UI..."
    for ($i = 1; $i -le 30; $i++) {
        try {
            Invoke-WebRequest -Uri "http://localhost:3000" -UseBasicParsing | Out-Null
            Write-Success "Web UI is ready!"
            break
        } catch {
            Write-Host "." -NoNewline
            Start-Sleep -Seconds 2
        }
    }
    Write-Host ""
    
    Write-Success "All services started successfully!"
}

# Create demo data
function Create-DemoData {
    Write-Info "Creating demo data..."
    
    # Run migrations
    Write-Info "Running database migrations..."
    docker-compose -p $ProjectName -f docker-compose.demo.yml exec -T api dotnet ef database update
    
    # Seed demo data via API
    Write-Info "Seeding demo accounts..."
    
    # Wait for API to be fully ready
    Start-Sleep -Seconds 5
    
    # Create admin user
    $adminBody = @{
        email = "admin@remotec.demo"
        password = "Admin@123"
        fullName = "Demo Admin"
        role = "Admin"
    } | ConvertTo-Json
    
    try {
        Invoke-RestMethod -Uri "http://localhost:7001/api/auth/register" -Method Post -Body $adminBody -ContentType "application/json"
    } catch {
        # Ignore if already exists
    }
    
    # Create regular user
    $userBody = @{
        email = "user@remotec.demo"
        password = "User@123"
        fullName = "Demo User"
        role = "User"
    } | ConvertTo-Json
    
    try {
        Invoke-RestMethod -Uri "http://localhost:7001/api/auth/register" -Method Post -Body $userBody -ContentType "application/json"
    } catch {
        # Ignore if already exists
    }
    
    Write-Success "Demo data created!"
}

# Configure Windows Firewall
function Configure-Firewall {
    param($MachineIP)
    
    Write-Info "Configuring Windows Firewall..."
    
    # Check if running as administrator
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    
    if ($isAdmin) {
        # Add firewall rules
        New-NetFirewallRule -DisplayName "RemoteC Web UI" -Direction Inbound -Protocol TCP -LocalPort 3000 -Action Allow -ErrorAction SilentlyContinue
        New-NetFirewallRule -DisplayName "RemoteC API" -Direction Inbound -Protocol TCP -LocalPort 7001 -Action Allow -ErrorAction SilentlyContinue
        Write-Success "Firewall rules configured!"
    } else {
        Write-Warning "Run as Administrator to configure firewall rules automatically"
        Write-Info "Please manually allow ports 3000 and 7001 in Windows Firewall"
    }
}

# Display access information
function Display-Info {
    param($MachineIP)
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "RemoteC Demo Deployment Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Access URLs:"
    Write-Host "  Web UI (Local):    " -NoNewline; Write-Host "http://localhost:3000" -ForegroundColor Blue
    Write-Host "  Web UI (Network):  " -NoNewline; Write-Host "http://${MachineIP}:3000" -ForegroundColor Blue
    Write-Host "  API (Local):       " -NoNewline; Write-Host "http://localhost:7001" -ForegroundColor Blue
    Write-Host "  API (Network):     " -NoNewline; Write-Host "http://${MachineIP}:7001" -ForegroundColor Blue
    Write-Host ""
    Write-Host "Demo Accounts:"
    Write-Host "  Admin: " -NoNewline; Write-Host "admin@remotec.demo / Admin@123" -ForegroundColor Yellow
    Write-Host "  User:  " -NoNewline; Write-Host "user@remotec.demo / User@123" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Services Status:"
    docker-compose -p $ProjectName -f docker-compose.demo.yml ps
    Write-Host ""
    Write-Host "Quick Commands:"
    Write-Host "  View logs:         docker-compose -p $ProjectName -f docker-compose.demo.yml logs -f"
    Write-Host "  Stop services:     docker-compose -p $ProjectName -f docker-compose.demo.yml down"
    Write-Host "  Restart services:  docker-compose -p $ProjectName -f docker-compose.demo.yml restart"
    Write-Host "  Clean everything:  docker-compose -p $ProjectName -f docker-compose.demo.yml down -v"
    Write-Host ""
    Write-Host "Note: For network access, ensure firewall allows ports 3000 and 7001" -ForegroundColor Yellow
}

# Main deployment flow
function Start-Deployment {
    Write-Info "Starting RemoteC demo deployment..."
    
    Set-Location $RootDir
    
    # Run deployment steps
    Check-Prerequisites
    $machineIP = Setup-Environment
    
    # Build and start
    Build-Images
    Start-Services
    Create-DemoData
    
    # Configure firewall
    Configure-Firewall -MachineIP $machineIP
    
    # Display access information
    Display-Info -MachineIP $machineIP
}

# Handle script actions
switch ($Action) {
    'start' {
        Start-Deployment
    }
    'stop' {
        Write-Info "Stopping RemoteC demo..."
        Set-Location $RootDir
        docker-compose -p $ProjectName -f docker-compose.demo.yml down
        Write-Success "Services stopped!"
    }
    'clean' {
        Write-Warning "Cleaning RemoteC demo (this will delete all data)..."
        Set-Location $RootDir
        docker-compose -p $ProjectName -f docker-compose.demo.yml down -v
        Remove-Item ".env" -ErrorAction SilentlyContinue
        Write-Success "Clean complete!"
    }
    'status' {
        Set-Location $RootDir
        docker-compose -p $ProjectName -f docker-compose.demo.yml ps
    }
    'logs' {
        Set-Location $RootDir
        docker-compose -p $ProjectName -f docker-compose.demo.yml logs -f
    }
}