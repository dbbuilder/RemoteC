#!/usr/bin/env pwsh
# Fast deployment using local builds instead of Docker builds
# This is 10-20x faster than building inside Docker

param(
    [switch]$Clean,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$startTime = Get-Date

# Script configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath

Write-Host ""
Write-Host "RemoteC Fast Deploy" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host ""

Set-Location $rootPath

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning existing deployment..." -ForegroundColor Yellow
    & "$scriptPath/docker-cleanup.ps1"
}

# Check .env file
if (-not (Test-Path ".env")) {
    Write-Host "Creating .env from template..." -ForegroundColor Yellow
    if (Test-Path ".env.template") {
        Copy-Item ".env.template" ".env"
        Write-Host ".env file created" -ForegroundColor Green
    } else {
        Write-Host ".env.template not found!" -ForegroundColor Red
        exit 1
    }
}

# Build if not skipped
if (-not $NoBuild) {
    Write-Host "Building application locally..." -ForegroundColor Yellow
    & "$scriptPath/build-local.ps1"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

# Stop existing containers
Write-Host ""
Write-Host "Stopping existing containers..." -ForegroundColor Yellow
$oldPreference = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"
& docker-compose -f docker-compose.demo.yml down 2>&1 | Out-Null
$ErrorActionPreference = $oldPreference

# Start services
Write-Host "Starting services..." -ForegroundColor Yellow

# Start Redis first
Write-Host "Starting Redis..." -ForegroundColor Gray

# Check if Redis image exists
$redisExists = docker images redis:alpine -q
if (-not $redisExists) {
    Write-Host "Pulling Redis image (first time only)..." -ForegroundColor Yellow
}

# Start Redis with output suppression for cleaner display
$ErrorActionPreference = "SilentlyContinue"
$redisOutput = docker-compose -f docker-compose.demo.yml up -d redis 2>&1
$ErrorActionPreference = "Stop"

# Show relevant output
if ($redisOutput -match "Creating|Started|done|Running") {
    Write-Host "Redis started successfully" -ForegroundColor Green
} elseif ($redisOutput -match "Pulling|Downloaded") {
    Write-Host "Redis image downloaded and started" -ForegroundColor Green
}

Start-Sleep -Seconds 2

# Start API
Write-Host "Starting API..." -ForegroundColor Gray
docker run -d `
    --name remotec-api `
    --network remotec-demo_remotec-network `
    -p 7001:8080 `
    -e ConnectionStrings__DefaultConnection="Server=sqltest.schoolvision.net,14333;Database=RemoteC2Db;User=sv;Password=Gv51076!;TrustServerCertificate=true" `
    -e ConnectionStrings__RedisConnection="remotec-redis:6379" `
    -e EnableDevAuth=true `
    -e ASPNETCORE_ENVIRONMENT=Development `
    -e Jwt__Secret="development-secret-key-for-testing-only-change-in-production" `
    -e Jwt__Issuer="RemoteC" `
    -e Jwt__Audience="RemoteC" `
    -e Cors__AllowedOrigins="http://localhost:3000" `
    remotec-api:latest

# Wait for API
Write-Host "Waiting for API to be ready..." -ForegroundColor Gray
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:7001/health" -TimeoutSec 2
        if ($response.status -eq "Healthy") {
            $ready = $true
            break
        }
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

# Start Web
Write-Host "Starting Web UI..." -ForegroundColor Gray
docker run -d `
    --name remotec-web `
    --network remotec-demo_remotec-network `
    -p 3000:80 `
    remotec-web:latest

# Wait for Web
Write-Host "Waiting for Web UI..." -ForegroundColor Gray
Start-Sleep -Seconds 3

# Test web
try {
    $webResponse = Invoke-WebRequest -Uri "http://localhost:3000" -UseBasicParsing -TimeoutSec 5
    if ($webResponse.StatusCode -eq 200) {
        Write-Host "Web UI is ready!" -ForegroundColor Green
    }
} catch {
    Write-Host "Web UI may still be starting..." -ForegroundColor Yellow
}

# Summary
$totalTime = (Get-Date) - $startTime
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "Total time: $($totalTime.TotalSeconds) seconds" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Services:" -ForegroundColor Cyan
Write-Host "  API:    http://localhost:7001" -ForegroundColor White
Write-Host "  Web UI: http://localhost:3000" -ForegroundColor White
Write-Host ""
Write-Host "Login:" -ForegroundColor Cyan
Write-Host "  Username: admin" -ForegroundColor White
Write-Host "  Password: admin123" -ForegroundColor White
Write-Host ""
Write-Host "Commands:" -ForegroundColor Cyan
Write-Host "  View API logs:  docker logs -f remotec-api" -ForegroundColor Gray
Write-Host "  View Web logs:  docker logs -f remotec-web" -ForegroundColor Gray
Write-Host "  Stop all:       docker stop remotec-api remotec-web remotec-redis" -ForegroundColor Gray
Write-Host ""

# Show running containers
Write-Host "Running containers:" -ForegroundColor Cyan
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | Select-String "remotec"