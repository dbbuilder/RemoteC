# Test API build only
Set-Location "D:\Dev2\remoteC"

Write-Host "Testing API build only..." -ForegroundColor Yellow

# Build just the API service
docker-compose -p remotec-demo -f docker-compose.demo.yml build api

Write-Host "`nExit code: $LASTEXITCODE" -ForegroundColor Cyan