#!/usr/bin/env pwsh
# Test network connectivity between laptop and desktop
# Run this on your laptop to test connection to desktop

param(
    [Parameter(Mandatory=$true)]
    [string]$DesktopIP  # Your desktop's IP address
)

Write-Host ""
Write-Host "RemoteC Network Connectivity Test" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Get laptop IP
$laptopIP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
    $_.InterfaceAlias -notlike "*Loopback*" -and 
    $_.InterfaceAlias -notlike "*vEthernet*" -and
    $_.IPAddress -notlike "169.254.*"
} | Select-Object -First 1).IPAddress

Write-Host "Test Configuration:" -ForegroundColor Yellow
Write-Host "  Laptop IP: $laptopIP" -ForegroundColor White
Write-Host "  Desktop IP: $DesktopIP" -ForegroundColor White
Write-Host ""

# Test 1: Ping
Write-Host "Test 1: Basic connectivity (ping)..." -ForegroundColor Yellow
$pingResult = Test-Connection -ComputerName $DesktopIP -Count 4 -Quiet
if ($pingResult) {
    Write-Host "  ✓ Ping successful" -ForegroundColor Green
} else {
    Write-Host "  ✗ Ping failed - check if desktop is reachable" -ForegroundColor Red
    Write-Host "  - Ensure both machines are on the same network" -ForegroundColor Gray
    Write-Host "  - Check Windows Firewall settings" -ForegroundColor Gray
}

# Test 2: API Port
Write-Host ""
Write-Host "Test 2: API port (7001)..." -ForegroundColor Yellow
$tcpClient = New-Object System.Net.Sockets.TcpClient
try {
    $tcpClient.Connect($DesktopIP, 7001)
    if ($tcpClient.Connected) {
        Write-Host "  ✓ Port 7001 is open" -ForegroundColor Green
        $tcpClient.Close()
    }
} catch {
    Write-Host "  ✗ Cannot connect to port 7001" -ForegroundColor Red
    Write-Host "  - Ensure RemoteC is running on desktop" -ForegroundColor Gray
    Write-Host "  - Check if Windows Firewall is blocking port 7001" -ForegroundColor Gray
}

# Test 3: API Health
Write-Host ""
Write-Host "Test 3: API health check..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://${DesktopIP}:7001/health" -TimeoutSec 5
    if ($response.status -eq "Healthy") {
        Write-Host "  ✓ API is healthy" -ForegroundColor Green
        Write-Host "  - Database: $($response.checks | Where-Object {$_.component -eq 'database'} | Select-Object -ExpandProperty status)" -ForegroundColor Gray
    } else {
        Write-Host "  ! API returned status: $($response.status)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ✗ Cannot reach API health endpoint" -ForegroundColor Red
    Write-Host "  - Error: $_" -ForegroundColor Gray
}

# Test 4: Web UI Port
Write-Host ""
Write-Host "Test 4: Web UI port (3000)..." -ForegroundColor Yellow
$tcpClient = New-Object System.Net.Sockets.TcpClient
try {
    $tcpClient.Connect($DesktopIP, 3000)
    if ($tcpClient.Connected) {
        Write-Host "  ✓ Port 3000 is open" -ForegroundColor Green
        $tcpClient.Close()
    }
} catch {
    Write-Host "  ✗ Cannot connect to port 3000" -ForegroundColor Red
}

# Test 5: Web UI
Write-Host ""
Write-Host "Test 5: Web UI accessibility..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://${DesktopIP}:3000" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✓ Web UI is accessible" -ForegroundColor Green
    }
} catch {
    Write-Host "  ✗ Cannot reach Web UI" -ForegroundColor Red
}

# Summary
Write-Host ""
Write-Host "Network Test Summary" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Desktop Firewall Rules (if needed):" -ForegroundColor Yellow
Write-Host "  New-NetFirewallRule -DisplayName 'RemoteC API' -Direction Inbound -Protocol TCP -LocalPort 7001 -Action Allow" -ForegroundColor Gray
Write-Host "  New-NetFirewallRule -DisplayName 'RemoteC Web' -Direction Inbound -Protocol TCP -LocalPort 3000 -Action Allow" -ForegroundColor Gray
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. If all tests pass, run: .\scripts\run-host-laptop.ps1 -DesktopIP $DesktopIP" -ForegroundColor White
Write-Host "  2. Access Web UI from laptop: http://${DesktopIP}:3000" -ForegroundColor White
Write-Host "  3. Login with admin/admin123" -ForegroundColor White
Write-Host "  4. Your laptop should appear in the Devices list" -ForegroundColor White
Write-Host ""