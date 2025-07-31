Write-Host "Starting RemoteC API..." -ForegroundColor Cyan

# Kill any existing processes on port 17001
$port = 17001
$process = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
if ($process) {
    Write-Host "Killing existing process on port $port..." -ForegroundColor Yellow
    Stop-Process -Id $process -Force
    Start-Sleep -Seconds 2
}

# Navigate to API directory
Set-Location "D:\Dev2\remoteC\src\RemoteC.Api"

# Set environment to Development
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:17001"

# Start the API
Write-Host "Starting API on http://localhost:17001" -ForegroundColor Green
dotnet run --no-launch-profile