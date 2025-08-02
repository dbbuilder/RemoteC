#!/usr/bin/env pwsh
# Quick script to open RemoteC ports in Windows Firewall
# Must run as Administrator

#Requires -RunAsAdministrator

Write-Host ""
Write-Host "Opening RemoteC Ports in Windows Firewall" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Define ports
$ports = @(
    @{Port = 7001; Name = "RemoteC API"; Description = "RemoteC API and SignalR Hub"},
    @{Port = 3000; Name = "RemoteC Web"; Description = "RemoteC Web Interface"}
)

# Create firewall rules
foreach ($portInfo in $ports) {
    Write-Host "Creating rule for port $($portInfo.Port)..." -ForegroundColor Yellow
    
    # Remove existing rule if present
    $existingRule = Get-NetFirewallRule -DisplayName $portInfo.Name -ErrorAction SilentlyContinue
    if ($existingRule) {
        Remove-NetFirewallRule -DisplayName $portInfo.Name
        Write-Host "  Removed existing rule" -ForegroundColor Gray
    }
    
    # Create new rule
    try {
        New-NetFirewallRule -DisplayName $portInfo.Name `
                           -Description $portInfo.Description `
                           -Direction Inbound `
                           -Protocol TCP `
                           -LocalPort $portInfo.Port `
                           -Action Allow `
                           -Profile Any `
                           -Enabled True | Out-Null
        
        Write-Host "  ✓ Created inbound rule for port $($portInfo.Port)" -ForegroundColor Green
    } catch {
        Write-Host "  ✗ Failed to create rule: $_" -ForegroundColor Red
    }
}

# Get machine IPs
Write-Host ""
Write-Host "Your machine is accessible at:" -ForegroundColor Cyan
$ips = Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
    $_.InterfaceAlias -notlike "*Loopback*" -and 
    $_.InterfaceAlias -notlike "*vEthernet*" -and
    $_.IPAddress -notlike "169.254.*"
}

foreach ($ip in $ips) {
    Write-Host "  http://$($ip.IPAddress):7001  (API)" -ForegroundColor White
    Write-Host "  http://$($ip.IPAddress):3000  (Web UI)" -ForegroundColor White
    Write-Host ""
}

Write-Host "Firewall rules created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Share one of the IP addresses above with your laptop" -ForegroundColor Gray
Write-Host "  2. On laptop, run: .\scripts\test-network.ps1 -DesktopIP <IP>" -ForegroundColor Gray
Write-Host "  3. If test passes, run: .\scripts\run-host-laptop.ps1 -DesktopIP <IP>" -ForegroundColor Gray
Write-Host ""