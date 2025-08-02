#!/usr/bin/env pwsh
# Local build script - builds outside Docker then creates minimal images
# This is MUCH faster than building inside Docker containers

param(
    [switch]$ApiOnly,
    [switch]$WebOnly,
    [switch]$SkipDocker
)

$ErrorActionPreference = "Stop"
$startTime = Get-Date

# Script configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath

Write-Host ""
Write-Host "RemoteC Local Build System" -ForegroundColor Cyan
Write-Host "==========================" -ForegroundColor Cyan
Write-Host "Building locally first, then creating Docker images" -ForegroundColor Gray
Write-Host ""

Set-Location $rootPath

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "Found .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host ".NET SDK not found. Please install from https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

# Check Node.js
try {
    $nodeVersion = node --version
    Write-Host "Found Node.js: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "Node.js not found. Please install from https://nodejs.org/" -ForegroundColor Red
    exit 1
}

# Build API
if (-not $WebOnly) {
    Write-Host ""
    Write-Host "Building API locally..." -ForegroundColor Yellow
    $apiBuildStart = Get-Date
    
    Set-Location "$rootPath/src/RemoteC.Api"
    
    # Restore packages
    Write-Host "Restoring NuGet packages..." -ForegroundColor Gray
    dotnet restore
    
    # Build in Release mode
    Write-Host "Building API..." -ForegroundColor Gray
    dotnet publish -c Release -o publish --no-restore
    
    $apiBuildTime = (Get-Date) - $apiBuildStart
    Write-Host "API built in $($apiBuildTime.TotalSeconds.ToString('0.0')) seconds" -ForegroundColor Green
    
    if (-not $SkipDocker) {
        # Create minimal Dockerfile for pre-built API
        Write-Host "Creating Docker image for API..." -ForegroundColor Yellow
        
        $dockerfileContent = @"
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app
COPY publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "RemoteC.Api.dll"]
"@
        
        Set-Content -Path "Dockerfile.minimal" -Value $dockerfileContent
        
        # Build Docker image with BuildKit
        $env:DOCKER_BUILDKIT = "1"
        docker build -f Dockerfile.minimal -t remotec-api:latest . 2>&1 | ForEach-Object {
            if ($_ -notmatch "DEPRECATED") {
                Write-Host $_
            }
        }
        
        # Clean up
        Remove-Item "Dockerfile.minimal"
        
        Write-Host "API Docker image created" -ForegroundColor Green
    }
    
    Set-Location $rootPath
}

# Build Web
if (-not $ApiOnly) {
    Write-Host ""
    Write-Host "Building Web UI locally..." -ForegroundColor Yellow
    $webBuildStart = Get-Date
    
    Set-Location "$rootPath/src/RemoteC.Web"
    
    # Check if node_modules exists
    if (-not (Test-Path "node_modules")) {
        Write-Host "Installing npm packages..." -ForegroundColor Gray
        npm install
    } else {
        Write-Host "npm packages already installed" -ForegroundColor Gray
    }
    
    # Build production bundle
    Write-Host "Building production bundle..." -ForegroundColor Gray
    npm run build
    
    $webBuildTime = (Get-Date) - $webBuildStart
    Write-Host "Web UI built in $($webBuildTime.TotalSeconds.ToString('0.0')) seconds" -ForegroundColor Green
    
    if (-not $SkipDocker) {
        # Create minimal Dockerfile for pre-built web
        Write-Host "Creating Docker image for Web..." -ForegroundColor Yellow
        
        # Ensure nginx.conf exists
        if (-not (Test-Path "nginx.conf")) {
            $nginxConfig = @"
events { 
    worker_connections 1024; 
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;
    
    server {
        listen 80;
        server_name localhost;
        root /usr/share/nginx/html;
        index index.html;
        
        location / {
            try_files `$uri `$uri/ /index.html;
        }
        
        location /api {
            proxy_pass http://remotec-api:8080;
            proxy_http_version 1.1;
            proxy_set_header Upgrade `$http_upgrade;
            proxy_set_header Connection 'upgrade';
            proxy_set_header Host `$host;
            proxy_cache_bypass `$http_upgrade;
        }
        
        location /hubs {
            proxy_pass http://remotec-api:8080;
            proxy_http_version 1.1;
            proxy_set_header Upgrade `$http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host `$host;
            proxy_cache_bypass `$http_upgrade;
        }
        
        location /health {
            return 200 "OK";
            add_header Content-Type text/plain;
        }
    }
}
"@
            Set-Content -Path "nginx.conf" -Value $nginxConfig
        }
        
        $dockerfileContent = @"
FROM nginx:alpine
COPY dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
"@
        
        Set-Content -Path "Dockerfile.minimal" -Value $dockerfileContent
        
        # Build Docker image with BuildKit
        $env:DOCKER_BUILDKIT = "1"
        docker build -f Dockerfile.minimal -t remotec-web:latest . 2>&1 | ForEach-Object {
            if ($_ -notmatch "DEPRECATED") {
                Write-Host $_
            }
        }
        
        # Clean up
        Remove-Item "Dockerfile.minimal"
        
        Write-Host "Web Docker image created" -ForegroundColor Green
    }
    
    Set-Location $rootPath
}

# Summary
$totalTime = (Get-Date) - $startTime
Write-Host ""
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "Total time: $($totalTime.TotalMinutes.ToString('0.0')) minutes" -ForegroundColor Green

if (-not $SkipDocker) {
    Write-Host ""
    Write-Host "Docker images created:" -ForegroundColor Cyan
    if (-not $WebOnly) {
        Write-Host "  - remotec-api:latest" -ForegroundColor White
    }
    if (-not $ApiOnly) {
        Write-Host "  - remotec-web:latest" -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Run: docker-compose -f docker-compose.demo.yml up -d" -ForegroundColor Gray
    Write-Host "  2. Access: http://localhost:3000" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "Local build complete. Docker images not created (use without -SkipDocker flag)" -ForegroundColor Yellow
}