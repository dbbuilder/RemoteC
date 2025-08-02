#!/usr/bin/env pwsh
# Docker cleanup script to remove old RemoteC images and containers

Write-Host ""
Write-Host "Docker Cleanup Script" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host ""

# Stop all RemoteC related containers
Write-Host "Stopping RemoteC containers..." -ForegroundColor Yellow
$containers = docker ps -a --format "table {{.ID}}\t{{.Names}}" | Select-String "remotec" | ForEach-Object { $_.ToString().Split()[0] }
if ($containers) {
    docker stop $containers 2>&1 | Out-Null
    docker rm $containers 2>&1 | Out-Null
    Write-Host "Removed $($containers.Count) RemoteC containers" -ForegroundColor Green
} else {
    Write-Host "No RemoteC containers to remove" -ForegroundColor Gray
}

# Remove RemoteC images
Write-Host ""
Write-Host "Removing RemoteC images..." -ForegroundColor Yellow
$images = @()

# Get all remotec related images
$allImages = docker images --format "{{.Repository}}:{{.Tag}}\t{{.ID}}" | Where-Object { $_ -match "remotec" }
foreach ($img in $allImages) {
    $parts = $img -split "\t"
    if ($parts.Count -ge 2) {
        $images += $parts[1]
        Write-Host "  Removing: $($parts[0])" -ForegroundColor Gray
    }
}

if ($images) {
    docker rmi -f $images 2>&1 | Out-Null
    Write-Host "Removed $($images.Count) RemoteC images" -ForegroundColor Green
} else {
    Write-Host "No RemoteC images to remove" -ForegroundColor Gray
}

# Remove dangling images
Write-Host ""
Write-Host "Removing dangling images..." -ForegroundColor Yellow
$danglingImages = docker images -f "dangling=true" -q
if ($danglingImages) {
    docker rmi $danglingImages 2>&1 | Out-Null
    Write-Host "Removed dangling images" -ForegroundColor Green
} else {
    Write-Host "No dangling images to remove" -ForegroundColor Gray
}

# Remove RemoteC volumes
Write-Host ""
Write-Host "Removing RemoteC volumes..." -ForegroundColor Yellow
$volumes = docker volume ls --format "{{.Name}}" | Where-Object { $_ -match "remotec" }
if ($volumes) {
    docker volume rm $volumes 2>&1 | Out-Null
    Write-Host "Removed $($volumes.Count) RemoteC volumes" -ForegroundColor Green
} else {
    Write-Host "No RemoteC volumes to remove" -ForegroundColor Gray
}

# Remove RemoteC networks
Write-Host ""
Write-Host "Removing RemoteC networks..." -ForegroundColor Yellow
$networks = docker network ls --format "{{.Name}}" | Where-Object { $_ -match "remotec" }
if ($networks) {
    docker network rm $networks 2>&1 | Out-Null
    Write-Host "Removed $($networks.Count) RemoteC networks" -ForegroundColor Green
} else {
    Write-Host "No RemoteC networks to remove" -ForegroundColor Gray
}

# Clean build cache
Write-Host ""
Write-Host "Cleaning Docker build cache..." -ForegroundColor Yellow
# Use buildx prune to avoid deprecation warning
$pruneOutput = docker buildx prune -f 2>&1
if ($LASTEXITCODE -ne 0) {
    # Fallback to system prune if buildx not available
    docker system prune -f --filter "label=stage=builder" 2>&1 | Out-Null
}
Write-Host "Build cache cleaned" -ForegroundColor Green

# Show disk usage
Write-Host ""
Write-Host "Current Docker disk usage:" -ForegroundColor Cyan
docker system df

# Ask about system prune
Write-Host ""
Write-Host "Do you want to run a full Docker system prune?" -ForegroundColor Yellow
Write-Host "This will remove:" -ForegroundColor Gray
Write-Host "  - All stopped containers" -ForegroundColor Gray
Write-Host "  - All networks not used by containers" -ForegroundColor Gray
Write-Host "  - All dangling images" -ForegroundColor Gray
Write-Host "  - All build cache" -ForegroundColor Gray
Write-Host ""
$response = Read-Host "Proceed with system prune? (y/N)"

if ($response -eq 'y' -or $response -eq 'Y') {
    Write-Host ""
    Write-Host "Running Docker system prune..." -ForegroundColor Yellow
    docker system prune -a -f --volumes
    Write-Host ""
    Write-Host "System prune complete!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Cleanup complete!" -ForegroundColor Green
Write-Host ""