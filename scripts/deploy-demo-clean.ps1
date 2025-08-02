#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Clean deployment script for RemoteC demo system
.DESCRIPTION
    This script performs a complete clean build and deployment of the RemoteC demo system,
    ensuring no cache or retained information from prior builds.
.PARAMETER SkipEnvCheck
    Skip the .env file check
.PARAMETER SkipDockerCheck
    Skip Docker daemon verification
.PARAMETER Force
    Force deployment even if containers are running
#>

param(
    [switch]$SkipEnvCheck,
    [switch]$SkipDockerCheck,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Script configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$envPath = Join-Path $rootPath ".env"
$dockerComposePath = Join-Path $rootPath "docker-compose.demo.yml"

# Colors for output
function Write-Info { Write-Host $args[0] -ForegroundColor Cyan }
function Write-Success { Write-Host $args[0] -ForegroundColor Green }
function Write-Warning { Write-Host $args[0] -ForegroundColor Yellow }
function Write-Error { Write-Host $args[0] -ForegroundColor Red }

# Banner
Write-Host ""
Write-Info "================================================"
Write-Info "  RemoteC Demo CLEAN Deployment Script"
Write-Info "  This will remove ALL cached data and rebuild"
Write-Info "================================================"
Write-Host ""

# Check if we're in the right directory
if (-not (Test-Path $dockerComposePath)) {
    Write-Error "docker-compose.demo.yml not found. Please run this script from the project root."
    exit 1
}

# Function to check if Docker is running
function Test-DockerRunning {
    $maxAttempts = 3
    $attempt = 0
    
    while ($attempt -lt $maxAttempts) {
        $attempt++
        try {
            # Try multiple methods to detect Docker
            $dockerVersion = docker version --format '{{.Server.Version}}' 2>$null
            if ($dockerVersion) {
                return $true
            }
            
            # Try docker info as backup
            $dockerInfo = docker info 2>&1
            if ($dockerInfo -notmatch "Cannot connect" -and $dockerInfo -notmatch "error") {
                return $true
            }
        }
        catch {
            # Ignore errors and try again
        }
        
        if ($attempt -lt $maxAttempts) {
            Start-Sleep -Seconds 2
        }
    }
    
    return $false
}

# Check Docker
if (-not $SkipDockerCheck) {
    Write-Info "Checking Docker..."
    if (-not (Test-DockerRunning)) {
        Write-Error "Docker is not running. Please start Docker Desktop and try again."
        exit 1
    }
    Write-Success "Docker is running"
}

# Check environment file
if (-not $SkipEnvCheck) {
    Write-Info "Checking environment configuration..."
    if (-not (Test-Path $envPath)) {
        Write-Warning ".env file not found. Creating from template..."
        $templatePath = Join-Path $rootPath ".env.template"
        if (Test-Path $templatePath) {
            Copy-Item $templatePath $envPath
            Write-Success "Created .env file from template"
        } else {
            Write-Error ".env.template not found. Cannot proceed."
            exit 1
        }
    } else {
        Write-Success ".env file exists"
    }
}

# Clean up existing deployment
Write-Host ""
Write-Warning "CLEANING UP EXISTING DEPLOYMENT..."

# Stop all containers
Write-Info "Stopping all RemoteC containers..."
$oldErrorAction = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"
docker-compose -f $dockerComposePath down 2>&1 | Out-Null
docker stop remotec-web remotec-api remotec-redis remotec-db 2>&1 | Out-Null
docker rm remotec-web remotec-api remotec-redis remotec-db 2>&1 | Out-Null
$ErrorActionPreference = $oldErrorAction

# Remove all RemoteC images
Write-Info "Removing all RemoteC images..."
$oldErrorAction = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"
docker rmi remotec-web:latest remotec-demo-web:latest 2>&1 | Out-Null
docker rmi remotec-api:latest remotec-demo-api:latest 2>&1 | Out-Null
$images = docker images -q -f "reference=remotec*" 2>&1
if ($images -and $images -ne "") {
    docker rmi $images 2>&1 | Out-Null
}
$ErrorActionPreference = $oldErrorAction

# Remove volumes
Write-Info "Removing Docker volumes..."
$oldErrorAction = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"
docker volume rm remotec_remotec-sqldata remotec_remotec-redis 2>&1 | Out-Null
docker volume prune -f 2>&1 | Out-Null
$ErrorActionPreference = $oldErrorAction

# Remove networks
Write-Info "Removing Docker networks..."
$oldErrorAction = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"
docker network rm remotec_remotec-network remotec-demo_remotec-network 2>&1 | Out-Null
$ErrorActionPreference = $oldErrorAction

# Clean Docker build cache
Write-Info "Cleaning Docker build cache..."
$oldErrorAction = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"
docker builder prune -f 2>&1 | Out-Null
$ErrorActionPreference = $oldErrorAction

Write-Success "Cleanup complete!"

# Load environment variables
Write-Host ""
Write-Info "Loading environment variables..."
Get-Content $envPath | ForEach-Object {
    if ($_ -match '^([^=]+)=(.*)$') {
        $key = $matches[1].Trim()
        $value = $matches[2].Trim()
        Set-Item -Path "env:$key" -Value $value
    }
}

# Create network first
Write-Host ""
Write-Info "Creating Docker network..."
$oldErrorAction = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"
$networkOutput = docker network create remotec-demo_remotec-network 2>&1
$networkExitCode = $LASTEXITCODE
$ErrorActionPreference = $oldErrorAction
if ($networkExitCode -eq 0) {
    Write-Success "Network created"
} else {
    Write-Warning "Network already exists or couldn't be created"
}

# Build services with no cache
Write-Host ""
Write-Info "Building services from scratch (no cache)..."

# Disable BuildKit for clearer output and to avoid warnings
$env:DOCKER_BUILDKIT = "0"
$env:COMPOSE_DOCKER_CLI_BUILD = "0"
$env:BUILDKIT_PROGRESS = "plain"

# Build API
Write-Info "Building API service..."
Write-Warning "This may take 5-10 minutes for a clean build..."
$startTime = Get-Date

# Run docker-compose build with real-time output
& docker-compose -f $dockerComposePath build --no-cache --progress=plain api 2>&1 | ForEach-Object {
    if ($_ -notmatch "buildkit isn't enabled") {
        Write-Host $_
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "API build failed!"
    exit 1
}
$duration = (Get-Date) - $startTime
Write-Success "API built in $($duration.TotalMinutes.ToString('0.0')) minutes"

# Build Web
Write-Info "Building Web service..."
Write-Warning "This may take 3-5 minutes for a clean build..."
$startTime = Get-Date

# Run docker-compose build with real-time output
& docker-compose -f $dockerComposePath build --no-cache --progress=plain web 2>&1 | ForEach-Object {
    if ($_ -notmatch "buildkit isn't enabled") {
        Write-Host $_
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Web build failed!"
    exit 1
}
$duration = (Get-Date) - $startTime
Write-Success "Web built in $($duration.TotalMinutes.ToString('0.0')) minutes"

# Start services
Write-Host ""
Write-Info "Starting services..."

# Start Redis first
Write-Info "Starting Redis..."
docker-compose -f $dockerComposePath up -d redis
Start-Sleep -Seconds 3

# Start API with correct port mapping
Write-Info "Starting API..."
docker-compose -f $dockerComposePath up -d api
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Docker Compose had issues, trying direct run..."
    docker run -d --name remotec-api `
        --network remotec-demo_remotec-network `
        -p 7001:8080 `
        -e ConnectionStrings__DefaultConnection="Server=sqltest.schoolvision.net,14333;Database=RemoteC2Db;User=sv;Password=Gv51076!;TrustServerCertificate=true" `
        -e EnableDevAuth=true `
        -e ASPNETCORE_ENVIRONMENT=Development `
        -e Jwt__Secret="development-secret-key-for-testing-only-change-in-production" `
        -e Cors__AllowedOrigins="http://localhost:3000" `
        remotec-api:latest
}

# Wait for API to be ready
Write-Info "Waiting for API to be ready..."
$attempts = 0
$maxAttempts = 30
while ($attempts -lt $maxAttempts) {
    $attempts++
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:7001/health" -UseBasicParsing -TimeoutSec 2 2>$null
        if ($response.StatusCode -eq 200) {
            Write-Success "API is ready!"
            break
        }
    }
    catch {
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 2
    }
}

# Start Web
Write-Info "Starting Web..."
docker-compose -f $dockerComposePath up -d web
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Docker Compose had issues, trying direct run..."
    docker run -d --name remotec-web `
        --network remotec-demo_remotec-network `
        -p 3000:80 `
        remotec-web:latest
}

# Verify deployment
Write-Host ""
Write-Info "Verifying deployment..."

# Check container status
$containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | Select-String "remotec"
if ($containers) {
    Write-Success "Containers are running:"
    $containers | ForEach-Object { Write-Host "   $_" }
} else {
    Write-Warning "No RemoteC containers found running"
}

# Test endpoints
Write-Host ""
Write-Info "Testing endpoints..."

# Test API
try {
    $apiHealth = Invoke-RestMethod -Uri "http://localhost:7001/health" -TimeoutSec 5
    Write-Success "API Health: $($apiHealth.status)"
} catch {
    Write-Warning "API health check failed"
}

# Test Web
try {
    $webResponse = Invoke-WebRequest -Uri "http://localhost:3000" -UseBasicParsing -TimeoutSec 5
    if ($webResponse.StatusCode -eq 200) {
        Write-Success "Web UI is accessible"
    }
} catch {
    Write-Warning "Web UI check failed"
}

# Display summary
Write-Host ""
Write-Info "================================================"
Write-Success "CLEAN DEPLOYMENT COMPLETE!"
Write-Info "================================================"
Write-Host ""
Write-Info "Services Status:"
Write-Info "  - API:    http://localhost:7001"
Write-Info "  - Web UI: http://localhost:3000"
Write-Info "  - Redis:  localhost:6379"
Write-Host ""
Write-Info "Login Credentials:"
Write-Info "  - Username: admin"
Write-Info "  - Password: admin123"
Write-Host ""
Write-Info "Useful Commands:"
Write-Info "  - View logs:     docker-compose -f docker-compose.demo.yml logs -f"
Write-Info "  - Stop all:      docker-compose -f docker-compose.demo.yml down"
Write-Info "  - Restart:       docker-compose -f docker-compose.demo.yml restart"
Write-Host ""
Write-Warning "Note: This deployment uses dev authentication mode"
Write-Warning "    Do not use in production!"
Write-Host ""