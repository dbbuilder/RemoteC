param(
    [string]$ServerUrl = "http://localhost:17001"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   Checking Server Status and Logs" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check health endpoint
Write-Host "[1] Checking server health at $ServerUrl/health" -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$ServerUrl/health" -Method GET
    Write-Host "✓ Server is healthy" -ForegroundColor Green
    Write-Host "  Status: $($healthResponse.status)" -ForegroundColor Gray
    Write-Host "  Total Duration: $($healthResponse.totalDuration)ms" -ForegroundColor Gray
    
    foreach ($check in $healthResponse.checks) {
        $icon = if ($check.status -eq "Healthy") { "✓" } else { "✗" }
        $color = if ($check.status -eq "Healthy") { "Green" } else { "Red" }
        Write-Host "  $icon $($check.component): $($check.status)" -ForegroundColor $color
    }
}
catch {
    Write-Host "✗ Server health check failed: $_" -ForegroundColor Red
}

Write-Host ""

# Try to get recent logs if available
Write-Host "[2] Checking for recent SignalR errors" -ForegroundColor Yellow
Write-Host "Please check the server console for any errors related to:" -ForegroundColor Gray
Write-Host "  - HostHub" -ForegroundColor Gray
Write-Host "  - RegisterHost method" -ForegroundColor Gray
Write-Host "  - SignalR connection" -ForegroundColor Gray
Write-Host "  - Serialization errors" -ForegroundColor Gray
Write-Host ""
Write-Host "Common issues to look for:" -ForegroundColor Yellow
Write-Host "  1. Missing dependencies (ISessionService, etc.)" -ForegroundColor Gray
Write-Host "  2. Database connection errors" -ForegroundColor Gray
Write-Host "  3. Serialization/deserialization errors" -ForegroundColor Gray
Write-Host "  4. Null reference exceptions" -ForegroundColor Gray