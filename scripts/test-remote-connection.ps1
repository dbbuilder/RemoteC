# RemoteC Connection Test Script
# Tests the connection between two machines

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerIP,
    
    [Parameter(Mandatory=$false)]
    [int]$ApiPort = 7001,
    
    [Parameter(Mandatory=$false)]
    [int]$SignalRPort = 7002
)

Write-Host "RemoteC Connection Test" -ForegroundColor Cyan
Write-Host "=======================" -ForegroundColor Cyan
Write-Host ""

$testsPassed = 0
$testsFailed = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET"
    )
    
    Write-Host "Testing $Name..." -ForegroundColor Yellow -NoNewline
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method $Method -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host " PASSED" -ForegroundColor Green
            $script:testsPassed++
            return $true
        } else {
            Write-Host " FAILED (Status: $($response.StatusCode))" -ForegroundColor Red
            $script:testsFailed++
            return $false
        }
    } catch {
        Write-Host " FAILED (Error: $($_.Exception.Message))" -ForegroundColor Red
        $script:testsFailed++
        return $false
    }
}

function Test-Port {
    param(
        [string]$Name,
        [string]$IP,
        [int]$Port
    )
    
    Write-Host "Testing $Name port..." -ForegroundColor Yellow -NoNewline
    
    $result = Test-NetConnection -ComputerName $IP -Port $Port -WarningAction SilentlyContinue
    if ($result.TcpTestSucceeded) {
        Write-Host " OPEN" -ForegroundColor Green
        $script:testsPassed++
        return $true
    } else {
        Write-Host " CLOSED" -ForegroundColor Red
        $script:testsFailed++
        return $false
    }
}

# Start tests
Write-Host "Server: $ServerIP" -ForegroundColor White
Write-Host "API Port: $ApiPort" -ForegroundColor White
Write-Host "SignalR Port: $SignalRPort" -ForegroundColor White
Write-Host ""

# Test network connectivity
Write-Host "1. Network Connectivity Tests" -ForegroundColor Cyan
Write-Host "-----------------------------" -ForegroundColor Cyan

Write-Host "Pinging server..." -ForegroundColor Yellow -NoNewline
$pingResult = Test-Connection -ComputerName $ServerIP -Count 2 -Quiet
if ($pingResult) {
    Write-Host " SUCCESS" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "Cannot reach server. Check network connection and firewall." -ForegroundColor Red
    $testsFailed++
}

# Test ports
Test-Port -Name "API" -IP $ServerIP -Port $ApiPort | Out-Null
Test-Port -Name "SignalR" -IP $ServerIP -Port $SignalRPort | Out-Null

Write-Host ""

# Test API endpoints
Write-Host "2. API Endpoint Tests" -ForegroundColor Cyan
Write-Host "---------------------" -ForegroundColor Cyan

$apiBase = "http://${ServerIP}:${ApiPort}"

Test-Endpoint -Name "Health Check" -Url "$apiBase/health" | Out-Null
Test-Endpoint -Name "API Root" -Url "$apiBase/" | Out-Null
Test-Endpoint -Name "Swagger UI" -Url "$apiBase/swagger" | Out-Null

Write-Host ""

# Test SignalR
Write-Host "3. SignalR Hub Test" -ForegroundColor Cyan
Write-Host "-------------------" -ForegroundColor Cyan

Test-Endpoint -Name "SignalR Hub" -Url "http://${ServerIP}:${SignalRPort}/hubs/session" | Out-Null

Write-Host ""

# Test WebSocket support
Write-Host "4. WebSocket Support Test" -ForegroundColor Cyan
Write-Host "-------------------------" -ForegroundColor Cyan

Write-Host "Checking WebSocket support..." -ForegroundColor Yellow -NoNewline
try {
    $ws = New-Object System.Net.WebSockets.ClientWebSocket
    Write-Host " SUPPORTED" -ForegroundColor Green
    $testsPassed++
    $ws.Dispose()
} catch {
    Write-Host " NOT SUPPORTED" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""

# Performance test
Write-Host "5. Performance Test" -ForegroundColor Cyan
Write-Host "-------------------" -ForegroundColor Cyan

Write-Host "Testing API response time..." -ForegroundColor Yellow -NoNewline
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $response = Invoke-WebRequest -Uri "$apiBase/health" -UseBasicParsing -ErrorAction Stop
    $stopwatch.Stop()
    $responseTime = $stopwatch.ElapsedMilliseconds
    
    if ($responseTime -lt 100) {
        Write-Host " EXCELLENT ($responseTime ms)" -ForegroundColor Green
        $testsPassed++
    } elseif ($responseTime -lt 500) {
        Write-Host " GOOD ($responseTime ms)" -ForegroundColor Yellow
        $testsPassed++
    } else {
        Write-Host " SLOW ($responseTime ms)" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host " FAILED" -ForegroundColor Red
    $testsFailed++
}

Write-Host ""

# Summary
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "============" -ForegroundColor Cyan
Write-Host "Tests Passed: $testsPassed" -ForegroundColor Green
Write-Host "Tests Failed: $testsFailed" -ForegroundColor Red

if ($testsFailed -eq 0) {
    Write-Host ""
    Write-Host "All tests passed! RemoteC server is ready." -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Deploy the Host component on machines you want to control" -ForegroundColor White
    Write-Host "2. Access the web interface at http://${ServerIP}:${ApiPort}" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "Some tests failed. Please check:" -ForegroundColor Yellow
    Write-Host "1. RemoteC server is running" -ForegroundColor White
    Write-Host "2. Firewall rules are configured" -ForegroundColor White
    Write-Host "3. Network connectivity between machines" -ForegroundColor White
}

# Optional: Test from Host perspective
$testHost = Read-Host "`nTest Host connection? (Y/N)"
if ($testHost -eq "Y") {
    Write-Host ""
    Write-Host "Host Connection Test" -ForegroundColor Cyan
    Write-Host "====================" -ForegroundColor Cyan
    
    # Simulate host connection
    Write-Host "Attempting to connect as Host..." -ForegroundColor Yellow
    
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    $hostInfo = @{
        MachineName = $env:COMPUTERNAME
        OSVersion = [System.Environment]::OSVersion.VersionString
        IpAddress = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" } | Select-Object -First 1).IPAddress
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$apiBase/api/devices/register" -Method POST -Body $hostInfo -Headers $headers -ErrorAction Stop
        Write-Host "Host registration: SUCCESS" -ForegroundColor Green
        Write-Host "Device ID: $($response.deviceId)" -ForegroundColor White
    } catch {
        Write-Host "Host registration: FAILED" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}