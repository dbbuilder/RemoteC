param(
    [string]$ServerUrl = "http://localhost:17001",
    [string]$HostId = "dev-host-001",
    [string]$HostSecret = "dev-secret-001"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   Testing SignalR Authentication Flow" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Get JWT Token
Write-Host "[1] Getting JWT token from $ServerUrl/api/auth/host/token" -ForegroundColor Yellow
$tokenBody = @{
    hostId = $HostId
    secret = $HostSecret
} | ConvertTo-Json

try {
    $tokenResponse = Invoke-RestMethod -Uri "$ServerUrl/api/auth/host/token" `
        -Method POST `
        -ContentType "application/json" `
        -Body $tokenBody
    
    Write-Host "✓ Successfully got token" -ForegroundColor Green
    Write-Host "  Token: $($tokenResponse.token.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host "  Type: $($tokenResponse.tokenType)" -ForegroundColor Gray
    Write-Host "  Expires in: $($tokenResponse.expiresIn) seconds" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Failed to get token: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Test SignalR Negotiation
Write-Host "[2] Testing SignalR negotiation at $ServerUrl/hubs/host/negotiate" -ForegroundColor Yellow
$headers = @{
    "Authorization" = "Bearer $($tokenResponse.token)"
}

try {
    # SignalR negotiation expects a specific version
    $negotiateUrl = "$ServerUrl/hubs/host/negotiate?negotiateVersion=1"
    $negotiateResponse = Invoke-RestMethod -Uri $negotiateUrl `
        -Method POST `
        -Headers $headers `
        -ContentType "application/json"
    
    Write-Host "✓ SignalR negotiation successful" -ForegroundColor Green
    Write-Host "  Connection ID: $($negotiateResponse.connectionId)" -ForegroundColor Gray
    Write-Host "  Available transports:" -ForegroundColor Gray
    foreach ($transport in $negotiateResponse.availableTransports) {
        Write-Host "    - $($transport.transport)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "✗ SignalR negotiation failed: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
        Write-Host "  Status Code: $statusCode" -ForegroundColor Red
        
        # Try to read the response body
        try {
            $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "  Response: $responseBody" -ForegroundColor Red
        }
        catch {
            Write-Host "  Could not read response body" -ForegroundColor Red
        }
    }
}

Write-Host ""

# Step 3: Test regular API endpoint with token
Write-Host "[3] Testing authenticated API access at $ServerUrl/api/devices" -ForegroundColor Yellow
try {
    $devicesResponse = Invoke-RestMethod -Uri "$ServerUrl/api/devices" `
        -Method GET `
        -Headers $headers
    
    Write-Host "✓ API authentication successful" -ForegroundColor Green
    Write-Host "  Retrieved $($devicesResponse.Count) devices" -ForegroundColor Gray
}
catch {
    Write-Host "✗ API authentication failed: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
        Write-Host "  Status Code: $statusCode" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Test complete." -ForegroundColor Cyan
