#!/usr/bin/env pwsh
# PowerShell script to build the web container without timing out

Write-Host "🚀 Building RemoteC Web Container" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Stop and remove existing container
Write-Host "`n📦 Stopping existing web container..." -ForegroundColor Yellow
docker stop remotec-web 2>$null
docker rm remotec-web 2>$null

# Build the web container
Write-Host "`n🔨 Building web container with dev auth enabled..." -ForegroundColor Yellow
Write-Host "This may take a few minutes..." -ForegroundColor Gray

$startTime = Get-Date
docker-compose -f docker-compose.demo.yml build web

if ($LASTEXITCODE -eq 0) {
    $duration = (Get-Date) - $startTime
    Write-Host "`n✅ Build completed successfully in $($duration.TotalMinutes.ToString('0.0')) minutes!" -ForegroundColor Green
    
    # Start the new container
    Write-Host "`n🚀 Starting web container..." -ForegroundColor Yellow
    docker-compose -f docker-compose.demo.yml up -d web
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Web container started successfully!" -ForegroundColor Green
        Write-Host "`nYou can now access the application at:" -ForegroundColor Cyan
        Write-Host "  http://localhost:3000" -ForegroundColor White
        Write-Host "`nLogin with:" -ForegroundColor Cyan
        Write-Host "  Username: admin" -ForegroundColor White
        Write-Host "  Password: admin123" -ForegroundColor White
    } else {
        Write-Host "`n❌ Failed to start web container" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "`n❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Show container status
Write-Host "`n📊 Container Status:" -ForegroundColor Yellow
docker ps | Select-String "remotec-"