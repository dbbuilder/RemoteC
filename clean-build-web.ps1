#!/usr/bin/env pwsh
# Clean build of web container

Write-Host "`n🧹 Cleaning up old builds..." -ForegroundColor Yellow
docker rmi remotec-web:latest 2>$null
docker rmi remotec-demo-web:latest 2>$null

Write-Host "`n🔨 Building web container from scratch..." -ForegroundColor Yellow
Write-Host "This ensures forceDevAuth is included..." -ForegroundColor Gray

$env:DOCKER_BUILDKIT = 0  # Disable BuildKit for clearer output

docker-compose -f docker-compose.demo.yml build --no-cache web

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Build successful!" -ForegroundColor Green
    
    # Start the container
    Write-Host "`n🚀 Starting web container..." -ForegroundColor Yellow
    docker run -d --name remotec-web --network remotec-demo_remotec-network -p 3000:80 remotec-web:latest
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Web container started!" -ForegroundColor Green
        
        # Verify the build includes our changes
        Write-Host "`n🔍 Verifying build..." -ForegroundColor Yellow
        Start-Sleep -Seconds 2
        
        $hasForceDevAuth = docker exec remotec-web grep -c "forceDevAuth" /usr/share/nginx/html/assets/index-*.js 2>$null
        if ($hasForceDevAuth -gt 0) {
            Write-Host "✅ forceDevAuth found in build" -ForegroundColor Green
        } else {
            Write-Host "⚠️  forceDevAuth NOT found - build may be cached" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "`n❌ Build failed!" -ForegroundColor Red
}