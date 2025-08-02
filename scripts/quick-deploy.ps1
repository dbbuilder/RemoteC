#!/usr/bin/env pwsh
# Quick deployment script with better progress indication

param(
    [switch]$SkipBuild,
    [switch]$ApiOnly,
    [switch]$WebOnly
)

$ErrorActionPreference = "Stop"

# Script configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath

Write-Host ""
Write-Host "RemoteC Quick Deploy" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host ""

# Change to root directory
Set-Location $rootPath

if (-not $SkipBuild) {
    if (-not $WebOnly) {
        Write-Host "Building API..." -ForegroundColor Yellow
        Write-Host "This typically takes 5-10 minutes for a clean build" -ForegroundColor Gray
        Write-Host "You'll see output like:" -ForegroundColor Gray
        Write-Host "  - Downloading base images" -ForegroundColor Gray
        Write-Host "  - Installing system packages" -ForegroundColor Gray
        Write-Host "  - Restoring NuGet packages" -ForegroundColor Gray
        Write-Host "  - Building the application" -ForegroundColor Gray
        Write-Host ""
        
        # Build with progress
        $env:DOCKER_BUILDKIT = "0"
        $env:COMPOSE_DOCKER_CLI_BUILD = "0"
        
        $process = Start-Process -FilePath "docker-compose" -ArgumentList "-f", "docker-compose.demo.yml", "build", "api" -NoNewWindow -PassThru
        
        # Show progress dots while building
        while (-not $process.HasExited) {
            Write-Host "." -NoNewline
            Start-Sleep -Seconds 5
        }
        Write-Host ""
        
        if ($process.ExitCode -ne 0) {
            Write-Host "API build failed!" -ForegroundColor Red
            exit 1
        }
        Write-Host "API build complete!" -ForegroundColor Green
    }
    
    if (-not $ApiOnly) {
        Write-Host ""
        Write-Host "Building Web..." -ForegroundColor Yellow
        Write-Host "This typically takes 3-5 minutes" -ForegroundColor Gray
        
        $process = Start-Process -FilePath "docker-compose" -ArgumentList "-f", "docker-compose.demo.yml", "build", "web" -NoNewWindow -PassThru
        
        while (-not $process.HasExited) {
            Write-Host "." -NoNewline
            Start-Sleep -Seconds 5
        }
        Write-Host ""
        
        if ($process.ExitCode -ne 0) {
            Write-Host "Web build failed!" -ForegroundColor Red
            exit 1
        }
        Write-Host "Web build complete!" -ForegroundColor Green
    }
}

# Start services
Write-Host ""
Write-Host "Starting services..." -ForegroundColor Yellow

# Stop existing containers
docker-compose -f docker-compose.demo.yml down 2>&1 | Out-Null

# Start Redis
docker-compose -f docker-compose.demo.yml up -d redis

if (-not $WebOnly) {
    # Start API
    Write-Host "Starting API..." -ForegroundColor Gray
    docker-compose -f docker-compose.demo.yml up -d api
    
    # Wait for API
    Write-Host "Waiting for API to be ready..." -ForegroundColor Gray
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        try {
            $null = Invoke-RestMethod -Uri "http://localhost:7001/health" -TimeoutSec 2
            $ready = $true
            break
        } catch {
            Write-Host "." -NoNewline
            Start-Sleep -Seconds 2
        }
    }
    Write-Host ""
    
    if ($ready) {
        Write-Host "API is ready!" -ForegroundColor Green
    } else {
        Write-Host "API failed to start!" -ForegroundColor Red
        docker logs remotec-api
        exit 1
    }
}

if (-not $ApiOnly) {
    # Start Web
    Write-Host "Starting Web..." -ForegroundColor Gray
    docker-compose -f docker-compose.demo.yml up -d web
    Write-Host "Web started!" -ForegroundColor Green
}

# Summary
Write-Host ""
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green
Write-Host ""
Write-Host "Services:" -ForegroundColor Cyan
Write-Host "  API:  http://localhost:7001" -ForegroundColor White
Write-Host "  Web:  http://localhost:3000" -ForegroundColor White
Write-Host ""
Write-Host "Login:" -ForegroundColor Cyan
Write-Host "  Username: admin" -ForegroundColor White
Write-Host "  Password: admin123" -ForegroundColor White
Write-Host ""
Write-Host "Commands:" -ForegroundColor Cyan
Write-Host "  View logs:  docker-compose -f docker-compose.demo.yml logs -f" -ForegroundColor Gray
Write-Host "  Stop all:   docker-compose -f docker-compose.demo.yml down" -ForegroundColor Gray
Write-Host ""