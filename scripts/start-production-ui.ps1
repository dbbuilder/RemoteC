param(
    [switch]$SkipDatabaseCheck,
    [switch]$UseRemoteDatabase,
    [string]$ApiUrl = "http://localhost:17001",
    [string]$UiPort = "17002"
)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   Starting RemoteC Production UI" -ForegroundColor Cyan
Write-Host "   (Simple Auth Mode)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
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
    Write-Host "✓ API server detected on port 17001" -ForegroundColor Green
}
else {
    Write-Host "WARNING: API server not detected on port 17001" -ForegroundColor Yellow
    Write-Host "Please start the API server first using start-full-stack-dev.ps1" -ForegroundColor Yellow
    $continue = Read-Host "Continue anyway? (Y/N)"
    if ($continue -ne 'Y') {
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
    Write-Host ""
    Write-Host "[1] Testing SQL Server connection..." -ForegroundColor Yellow
    
    $dbServer = if ($UseRemoteDatabase) { "sqltest.schoolvision.net,14333" } else { "172.31.208.1,14333" }
    $dbTest = sqlcmd -S $dbServer -U sv -P Gv51076! -C -Q "SELECT 'OK' as Status" -d master -h -1 2>$null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Cannot connect to SQL Server at $dbServer!" -ForegroundColor Red
        Write-Host "Try with -UseRemoteDatabase flag for remote SQL server" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "✓ Database connection successful" -ForegroundColor Green
}

# Start UI in production mode
if (-not $uiRunning) {
    Write-Host ""
    Write-Host "[2] Starting React UI in Production Mode on http://localhost:$UiPort" -ForegroundColor Yellow
    
    $uiPath = Join-Path $PSScriptRoot "..\src\RemoteC.Web"
    
    # Create env file for simple auth
    $envContent = @"
VITE_USE_SIMPLE_AUTH=true
VITE_API_URL=$ApiUrl
"@
    
    $envPath = Join-Path $uiPath ".env.local"
    $envContent | Out-File -FilePath $envPath -Encoding UTF8
    
    Write-Host "✓ Created .env.local with simple auth configuration" -ForegroundColor Green
    
    # Start the UI with simple auth
    $uiProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", @"
cd '$uiPath'
Write-Host 'Starting RemoteC UI in Production Mode...' -ForegroundColor Green
Write-Host 'Simple Auth Enabled - Use test credentials to login' -ForegroundColor Yellow
npm run dev
"@ -PassThru
    
    # Wait a moment for the UI to start
    Start-Sleep -Seconds 3
}
else {
    Write-Host "✓ Using existing UI on port $UiPort" -ForegroundColor Green
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "    Production UI Running!" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "UI:          http://localhost:$UiPort" -ForegroundColor Yellow
Write-Host "API Server:  $ApiUrl" -ForegroundColor Yellow
Write-Host ""
Write-Host "Login Credentials:" -ForegroundColor Cyan
Write-Host "  Username: admin     Password: admin123     (Full access)" -ForegroundColor Gray
Write-Host "  Username: operator  Password: operator123  (Operator access)" -ForegroundColor Gray
Write-Host "  Username: viewer    Password: viewer123    (Read-only access)" -ForegroundColor Gray
Write-Host ""
Write-Host "The UI should open in your browser automatically." -ForegroundColor Gray
Write-Host "Press Ctrl+C to stop this coordinator (services will continue running)" -ForegroundColor Gray

# Keep the script running
while ($true) {
    Start-Sleep -Seconds 60
}