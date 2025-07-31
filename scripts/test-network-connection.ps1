# RemoteC Network Connection Test Script

param(
    [string]$ServerIP = "localhost",
    [int]$Port = 17001
)

Write-Host "RemoteC Network Connection Test" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host

# Test network connectivity
Write-Host "Testing connection to $ServerIP`:$Port..." -ForegroundColor Yellow
$result = Test-NetConnection -ComputerName $ServerIP -Port $Port -WarningAction SilentlyContinue

if ($result.TcpTestSucceeded) {
    Write-Host "[OK] TCP connection successful!" -ForegroundColor Green
    
    # Test API endpoint
    Write-Host "`nTesting API health endpoint..." -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Uri "http://${ServerIP}:${Port}/health" -Method Get
        Write-Host "[OK] API is healthy!" -ForegroundColor Green
        Write-Host "  Status: $($response.status)" -ForegroundColor Gray
        $dbCheck = $response.checks | Where-Object {$_.component -eq 'database'}
        if ($dbCheck) {
            Write-Host "  Database: $($dbCheck.status)" -ForegroundColor Gray
        }
    } catch {
        Write-Host "[FAIL] API health check failed" -ForegroundColor Red
        Write-Host "  Error: $_" -ForegroundColor Red
    }
} else {
    Write-Host "[FAIL] Cannot connect to server" -ForegroundColor Red
    Write-Host "  Please check:" -ForegroundColor Yellow
    Write-Host "  1. Server is running on $ServerIP" -ForegroundColor White
    Write-Host "  2. Port $Port is not blocked by firewall" -ForegroundColor White
    Write-Host "  3. Server is listening on all interfaces (0.0.0.0)" -ForegroundColor White
}

Write-Host "`nNetwork Information:" -ForegroundColor Cyan
Write-Host "Local IP Addresses:" -ForegroundColor Yellow
Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.InterfaceAlias -notlike "*Loopback*"} | 
    ForEach-Object { Write-Host "  $($_.InterfaceAlias): $($_.IPAddress)" -ForegroundColor Gray }