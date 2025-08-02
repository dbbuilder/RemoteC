# Test build script to see full error
Set-Location "D:\Dev2\remoteC"

Write-Host "Running docker-compose build with full output..." -ForegroundColor Yellow

# Run the command and capture all output
$process = Start-Process -FilePath "docker-compose" `
    -ArgumentList @("-p", "remotec-demo", "-f", "docker-compose.demo.yml", "build", "--parallel") `
    -NoNewWindow `
    -RedirectStandardOutput "build-output.txt" `
    -RedirectStandardError "build-error.txt" `
    -Wait `
    -PassThru

Write-Host "Exit code: $($process.ExitCode)" -ForegroundColor Cyan

Write-Host "`nStandard Output:" -ForegroundColor Green
Get-Content "build-output.txt" -ErrorAction SilentlyContinue

Write-Host "`nStandard Error:" -ForegroundColor Red
Get-Content "build-error.txt" -ErrorAction SilentlyContinue

Write-Host "`nCheck build-output.txt and build-error.txt for full details" -ForegroundColor Yellow