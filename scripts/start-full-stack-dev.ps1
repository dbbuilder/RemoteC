param(
    [switch]$SkipDatabaseCheck,
    [switch]$UseRemoteDatabase,
    [string]$ApiUrl = "http://localhost:17001",
    [string]$UiPort = "17002"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   Starting RemoteC Full Stack (Dev Mode)" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Function to test port availability
function Test-Port {
    param($Port)
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    try {
        $tcpClient.Connect("localhost", $Port)
        $tcpClient.Close()
        return $true
    }
    catch {
        return $false
    }
}

# Check if services are already running
$apiRunning = Test-Port 17001
$uiRunning = Test-Port $UiPort

if ($apiRunning) {
    Write-Host "WARNING: API server already running on port 17001" -ForegroundColor Yellow
    Write-Host "Stop it first or use existing instance? (S/U)" -ForegroundColor Yellow
    $choice = Read-Host
    if ($choice -eq 'S') {
        Write-Host "Please stop the existing API server and run this script again." -ForegroundColor Red
        exit 1
    }
}

if ($uiRunning) {
    Write-Host "WARNING: UI already running on port $UiPort" -ForegroundColor Yellow
    Write-Host "Stop it first or use existing instance? (S/U)" -ForegroundColor Yellow
    $choice = Read-Host
    if ($choice -eq 'S') {
        Write-Host "Please stop the existing UI server and run this script again." -ForegroundColor Red
        exit 1
    }
}

# Test database connection
if (-not $SkipDatabaseCheck) {
    Write-Host "[1] Testing SQL Server connection..." -ForegroundColor Yellow
    
    $dbServer = if ($UseRemoteDatabase) { "sqltest.schoolvision.net,14333" } else { "172.31.208.1,14333" }
    $dbTest = sqlcmd -S $dbServer -U sv -P Gv51076! -C -Q "SELECT 'OK' as Status" -d master -h -1 2>$null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Cannot connect to SQL Server at $dbServer!" -ForegroundColor Red
        Write-Host "Try with -UseRemoteDatabase flag for remote SQL server" -ForegroundColor Yellow
        exit 1
    }
    Write-Host " Database connection successful" -ForegroundColor Green
}

# Start API if not already running
if (-not $apiRunning) {
    Write-Host ""
    Write-Host "[2] Starting API Server on $ApiUrl" -ForegroundColor Yellow
    
    $apiPath = Join-Path $PSScriptRoot "..\src\RemoteC.Api"
    $apiProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$apiPath'; Write-Host 'Starting RemoteC API Server...' -ForegroundColor Green; dotnet run" -PassThru
    
    # Wait for API to start
    Write-Host "Waiting for API server to start..." -ForegroundColor Gray
    $attempts = 0
    while ($attempts -lt 30) {
        Start-Sleep -Seconds 1
        try {
            $response = Invoke-RestMethod -Uri "$ApiUrl/health" -Method Get -ErrorAction SilentlyContinue
            if ($response.status -eq "Healthy") {
                Write-Host " API server is ready" -ForegroundColor Green
                break
            }
        }
        catch {
            # API not ready yet
        }
        $attempts++
    }
    
    if ($attempts -eq 30) {
        Write-Host "WARNING: API server may not be ready yet" -ForegroundColor Yellow
    }
}
else {
    Write-Host " Using existing API server on port 17001" -ForegroundColor Green
}

# Start UI if not already running
if (-not $uiRunning) {
    Write-Host ""
    Write-Host "[3] Starting React UI on http://localhost:$UiPort" -ForegroundColor Yellow
    
    $uiPath = Join-Path $PSScriptRoot "..\src\RemoteC.Web"
    
    # Set environment variable for API URL if needed
    $env:REACT_APP_API_URL = $ApiUrl
    
    $uiProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$uiPath'; Write-Host 'Starting RemoteC UI...' -ForegroundColor Green; npm run dev" -PassThru
}
else {
    Write-Host " Using existing UI on port $UiPort" -ForegroundColor Green
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "    All services running!" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "API Server:  $ApiUrl" -ForegroundColor Yellow
Write-Host "UI:          http://localhost:$UiPort" -ForegroundColor Yellow
Write-Host "Health:      $ApiUrl/health" -ForegroundColor Yellow
Write-Host "Swagger:     $ApiUrl/swagger" -ForegroundColor Yellow
Write-Host ""
Write-Host "The UI should open in your browser automatically." -ForegroundColor Gray
Write-Host "Default login: any username/password in dev mode" -ForegroundColor Gray
Write-Host ""
Write-Host "Press Ctrl+C to stop this coordinator (services will continue running)" -ForegroundColor Gray