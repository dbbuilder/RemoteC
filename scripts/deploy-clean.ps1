#!/usr/bin/env pwsh
# Clean deployment script with suppressed Docker output

param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$startTime = Get-Date

# Script configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath

Write-Host ""
Write-Host "RemoteC Clean Deploy" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host ""

Set-Location $rootPath

# Helper function to run Docker commands quietly
function Invoke-DockerQuietly {
    param($Command)
    $oldPreference = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"
    $output = Invoke-Expression $Command 2>&1
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $oldPreference
    return @{
        Output = $output
        ExitCode = $exitCode
    }
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
    Write-Host "Building application..." -ForegroundColor Yellow
    & "$scriptPath/build-local.ps1"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

# Clean up
Write-Host ""
Write-Host "Cleaning up..." -ForegroundColor Yellow

# Stop all containers
$result = Invoke-DockerQuietly "docker-compose -f docker-compose.demo.yml down"
$result = Invoke-DockerQuietly "docker stop remotec-api remotec-web remotec-redis"
$result = Invoke-DockerQuietly "docker rm remotec-api remotec-web remotec-redis"

Write-Host "Cleanup complete" -ForegroundColor Green

# Create network if needed
Write-Host "Setting up network..." -ForegroundColor Yellow
$result = Invoke-DockerQuietly "docker network create remotec-demo_remotec-network"
if ($result.ExitCode -eq 0) {
    Write-Host "Network created" -ForegroundColor Green
} else {
    Write-Host "Network already exists" -ForegroundColor Gray
}

# Start services
Write-Host "Starting services..." -ForegroundColor Yellow

# Start Redis
Write-Host "  - Starting Redis..." -ForegroundColor Gray
$result = Invoke-DockerQuietly "docker run -d --name remotec-redis --network remotec-demo_remotec-network -p 6379:6379 redis:alpine"
if ($result.ExitCode -eq 0) {
    Write-Host "    Redis started" -ForegroundColor Green
} else {
    # Try to pull and run
    Write-Host "    Pulling Redis image..." -ForegroundColor Yellow
    docker pull redis:alpine | Out-Host
    $result = Invoke-DockerQuietly "docker run -d --name remotec-redis --network remotec-demo_remotec-network -p 6379:6379 redis:alpine"
}

Start-Sleep -Seconds 2

# Start API
Write-Host "  - Starting API..." -ForegroundColor Gray
$result = Invoke-DockerQuietly @"
docker run -d --name remotec-api --network remotec-demo_remotec-network -p 7001:8080 -e ConnectionStrings__DefaultConnection='Server=sqltest.schoolvision.net,14333;Database=RemoteC2Db;User=sv;Password=Gv51076!;TrustServerCertificate=true' -e ConnectionStrings__RedisConnection='remotec-redis:6379' -e EnableDevAuth=true -e ASPNETCORE_ENVIRONMENT=Development -e Jwt__Secret='development-secret-key-for-testing-only-change-in-production' -e Jwt__Issuer='RemoteC' -e Jwt__Audience='RemoteC' -e Cors__AllowedOrigins='http://localhost:3000' remotec-api:latest
"@

if ($result.ExitCode -eq 0) {
    Write-Host "    API started" -ForegroundColor Green
} else {
    Write-Host "    API failed to start!" -ForegroundColor Red
    docker logs remotec-api | Out-Host
    exit 1
}

# Wait for API
Write-Host "  - Waiting for API..." -ForegroundColor Gray
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:7001/health" -TimeoutSec 2
        if ($response.status -eq "Healthy") {
            $ready = $true
            Write-Host "    API is ready!" -ForegroundColor Green
            break
        }
    } catch {
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 2
    }
}
if (-not $ready) {
    Write-Host ""
    Write-Host "    API health check failed!" -ForegroundColor Red
    docker logs remotec-api | Out-Host
    exit 1
}

# Start Web
Write-Host "  - Starting Web UI..." -ForegroundColor Gray
$result = Invoke-DockerQuietly "docker run -d --name remotec-web --network remotec-demo_remotec-network -p 3000:80 remotec-web:latest"

if ($result.ExitCode -eq 0) {
    Write-Host "    Web UI started" -ForegroundColor Green
} else {
    Write-Host "    Web UI failed to start!" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 3

# Summary
$totalTime = (Get-Date) - $startTime
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "Time: $($totalTime.TotalSeconds) seconds" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Access:" -ForegroundColor Cyan
Write-Host "  Web UI: http://localhost:3000" -ForegroundColor White
Write-Host "  API:    http://localhost:7001" -ForegroundColor White
Write-Host ""
Write-Host "Login:" -ForegroundColor Cyan
Write-Host "  Username: admin" -ForegroundColor White
Write-Host "  Password: admin123" -ForegroundColor White
Write-Host ""

# Show container status
Write-Host "Container Status:" -ForegroundColor Cyan
docker ps --format "table {{.Names}}\t{{.Status}}" | Select-String "remotec|NAMES" | Out-Host