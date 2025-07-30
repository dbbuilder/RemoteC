# Test RemoteC Host Connection Script

param(
    [string]$ServerUrl = "http://10.0.0.91:17001",
    [string]$HostId = "dev-host-001",
    [string]$HostSecret = "dev-secret-key"
)

Write-Host "Testing RemoteC Host Connection" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host

# Test 1: Basic connectivity
Write-Host "1. Testing server connectivity..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$ServerUrl/health" -Method Get
    Write-Host "✓ Server is healthy: $($healthResponse.status)" -ForegroundColor Green
} catch {
    Write-Host "✗ Cannot reach server at $ServerUrl" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    exit 1
}

# Test 2: Try to get auth token
Write-Host "`n2. Testing authentication..." -ForegroundColor Yellow
$authBody = @{
    hostId = $HostId
    secret = $HostSecret
} | ConvertTo-Json

try {
    $tokenResponse = Invoke-RestMethod -Uri "$ServerUrl/api/auth/host/token" -Method Post -Body $authBody -ContentType "application/json"
    Write-Host "✓ Authentication successful" -ForegroundColor Green
    Write-Host "  Token: $($tokenResponse.token.Substring(0, 20))..." -ForegroundColor Gray
} catch {
    Write-Host "✗ Authentication failed" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    Write-Host "  Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    
    if ($_.Exception.Response.StatusCode.value__ -eq 404) {
        Write-Host "`n  The endpoint /api/auth/host/token was not found." -ForegroundColor Yellow
        Write-Host "  This might need to be implemented in the AuthController." -ForegroundColor Yellow
    }
}

# Test 3: Check SignalR negotiate endpoint
Write-Host "`n3. Testing SignalR negotiate endpoint..." -ForegroundColor Yellow
try {
    # SignalR negotiate requires authentication, so this might fail
    $negotiateResponse = Invoke-RestMethod -Uri "$ServerUrl/hubs/host/negotiate?negotiateVersion=1" -Method Post
    Write-Host "✓ SignalR negotiate endpoint accessible" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 401) {
        Write-Host "✓ SignalR endpoint exists (requires authentication)" -ForegroundColor Green
    } elseif ($_.Exception.Response.StatusCode.value__ -eq 404) {
        Write-Host "✗ SignalR hub not found at /hubs/host" -ForegroundColor Red
        Write-Host "  The HostHub needs to be registered in Program.cs" -ForegroundColor Yellow
    } else {
        Write-Host "✗ SignalR negotiate failed" -ForegroundColor Red
        Write-Host "  Error: $_" -ForegroundColor Red
    }
}

Write-Host "`nSummary:" -ForegroundColor Cyan
Write-Host "- Server URL: $ServerUrl" -ForegroundColor White
Write-Host "- Host ID: $HostId" -ForegroundColor White
Write-Host "- Make sure the API server has been restarted after adding HostHub" -ForegroundColor Yellow