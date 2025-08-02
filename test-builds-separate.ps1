# Test builds separately
Set-Location "D:\Dev2\remoteC"

Write-Host "Testing Web build..." -ForegroundColor Yellow
docker-compose -p remotec-demo -f docker-compose.demo.yml build web

Write-Host "`nWeb build exit code: $LASTEXITCODE" -ForegroundColor Cyan

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nTesting API build..." -ForegroundColor Yellow
    docker-compose -p remotec-demo -f docker-compose.demo.yml build api
    Write-Host "`nAPI build exit code: $LASTEXITCODE" -ForegroundColor Cyan
}