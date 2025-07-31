param(
    [string]$ServerUrl = "http://localhost:17001",
    [string]$HostId = "dev-host-001",
    [string]$HostSecret = "dev-secret-001",
    [string]$TokenEndpoint = ""
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   Starting RemoteC Host with Parameters" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# If TokenEndpoint not specified, construct it from ServerUrl
if ([string]::IsNullOrEmpty($TokenEndpoint)) {
    $TokenEndpoint = "$ServerUrl/api/auth/host/token"
}

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "- Server URL: $ServerUrl"
Write-Host "- Host ID: $HostId"
Write-Host "- Host Secret: $HostSecret"
Write-Host "- Token Endpoint: $TokenEndpoint"
Write-Host ""

# Navigate to host directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$hostPath = Join-Path $scriptPath "..\src\RemoteC.Host"
Set-Location $hostPath

# Run the host with command line parameters
Write-Host "Starting host with command line configuration..." -ForegroundColor Green
$arguments = @(
    "--server", $ServerUrl,
    "--id", $HostId,
    "--secret", $HostSecret,
    "--token-endpoint", $TokenEndpoint
)

& dotnet run -- $arguments