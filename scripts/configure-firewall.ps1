# RemoteC Firewall Configuration Script
# Run as Administrator

Write-Host "Configuring Windows Firewall for RemoteC..." -ForegroundColor Green

# Remove existing rules if any
Remove-NetFirewallRule -DisplayName "RemoteC API" -ErrorAction SilentlyContinue
Remove-NetFirewallRule -DisplayName "RemoteC SignalR" -ErrorAction SilentlyContinue

# Add inbound rule for API
New-NetFirewallRule -DisplayName "RemoteC API" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 17001 `
    -Action Allow `
    -Profile Domain,Private `
    -Description "Allow RemoteC API traffic on port 17001"

Write-Host "Firewall rule created for port 17001" -ForegroundColor Yellow

# Test if port is open
Write-Host "`nTesting port accessibility..." -ForegroundColor Green
$listener = [System.Net.Sockets.TcpListener]17001
try {
    $listener.Start()
    Write-Host "Port 17001 is available and firewall is configured correctly" -ForegroundColor Green
    $listener.Stop()
} catch {
    Write-Host "Warning: Port 17001 might be in use or blocked" -ForegroundColor Red
}

Write-Host "`nFirewall configuration complete!" -ForegroundColor Green
Write-Host "You can now run the RemoteC server on port 17001" -ForegroundColor Cyan