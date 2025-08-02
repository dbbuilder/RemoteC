#!/usr/bin/env pwsh
# Check Windows Firewall status for RemoteC ports
# Run this on your desktop to verify firewall configuration

param(
    [switch]$Fix  # Automatically create rules if missing
)

# Check if running as admin
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

Write-Host ""
Write-Host "RemoteC Firewall Status Check" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan
Write-Host ""

if (-not $isAdmin -and $Fix) {
    Write-Host "Administrator privileges required to modify firewall rules" -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator" -ForegroundColor Yellow
    exit 1
}

# Get current machine IP addresses
$ipAddresses = Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
    $_.InterfaceAlias -notlike "*Loopback*" -and 
    $_.InterfaceAlias -notlike "*vEthernet*" -and
    $_.IPAddress -notlike "169.254.*"
} | Select-Object -ExpandProperty IPAddress

Write-Host "Machine IP Addresses:" -ForegroundColor Yellow
foreach ($ip in $ipAddresses) {
    Write-Host "  - $ip" -ForegroundColor White
}
Write-Host ""

# Check if Windows Firewall is enabled
Write-Host "Firewall Status:" -ForegroundColor Yellow
$firewallProfiles = Get-NetFirewallProfile
foreach ($profile in $firewallProfiles) {
    $status = if ($profile.Enabled) { "Enabled" } else { "Disabled" }
    $color = if ($profile.Enabled) { "Green" } else { "Gray" }
    Write-Host "  $($profile.Name): $status" -ForegroundColor $color
}
Write-Host ""

# Check for RemoteC specific rules
Write-Host "RemoteC Firewall Rules:" -ForegroundColor Yellow
$remoteCRules = @()
$requiredPorts = @(
    @{Port = 7001; Name = "RemoteC API"; Description = "RemoteC API and SignalR Hub"},
    @{Port = 3000; Name = "RemoteC Web UI"; Description = "RemoteC Web Interface"},
    @{Port = 6379; Name = "RemoteC Redis"; Description = "Redis Cache (optional)"}
)

foreach ($portInfo in $requiredPorts) {
    $rules = Get-NetFirewallRule -DisplayName "*$($portInfo.Port)*" -ErrorAction SilentlyContinue
    if (-not $rules) {
        $rules = Get-NetFirewallPortFilter | Where-Object { $_.LocalPort -eq $portInfo.Port } | 
                 ForEach-Object { Get-NetFirewallRule -AssociatedNetFirewallPortFilter $_ } -ErrorAction SilentlyContinue
    }
    
    $found = $false
    foreach ($rule in $rules) {
        if ($rule.Enabled -eq $true -and $rule.Direction -eq "Inbound" -and $rule.Action -eq "Allow") {
            Write-Host "  ✓ Port $($portInfo.Port) ($($portInfo.Name)): " -NoNewline -ForegroundColor Green
            Write-Host "Rule '$($rule.DisplayName)' is active" -ForegroundColor Gray
            $found = $true
            $remoteCRules += $rule
        }
    }
    
    if (-not $found) {
        Write-Host "  ✗ Port $($portInfo.Port) ($($portInfo.Name)): " -NoNewline -ForegroundColor Red
        Write-Host "No active inbound rule found" -ForegroundColor Gray
    }
}

# Check if ports are actually listening
Write-Host ""
Write-Host "Port Listening Status:" -ForegroundColor Yellow
$listeningPorts = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | 
                  Where-Object { $_.LocalPort -in @(7001, 3000, 6379) }

foreach ($portInfo in $requiredPorts) {
    $listening = $listeningPorts | Where-Object { $_.LocalPort -eq $portInfo.Port }
    if ($listening) {
        Write-Host "  ✓ Port $($portInfo.Port): " -NoNewline -ForegroundColor Green
        Write-Host "LISTENING (Process ID: $($listening.OwningProcess))" -ForegroundColor Gray
        
        # Try to get process name
        try {
            $process = Get-Process -Id $listening.OwningProcess -ErrorAction SilentlyContinue
            if ($process) {
                Write-Host "    Process: $($process.ProcessName)" -ForegroundColor Gray
            }
        } catch {}
    } else {
        Write-Host "  - Port $($portInfo.Port): " -NoNewline -ForegroundColor Yellow
        Write-Host "Not listening (service may not be running)" -ForegroundColor Gray
    }
}

# Test external connectivity
Write-Host ""
Write-Host "External Connectivity Test:" -ForegroundColor Yellow
Write-Host "Testing if ports are accessible from localhost..." -ForegroundColor Gray

foreach ($portInfo in $requiredPorts | Where-Object { $_.Port -ne 6379 }) {
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $tcpClient.Connect("localhost", $portInfo.Port)
        if ($tcpClient.Connected) {
            Write-Host "  ✓ localhost:$($portInfo.Port) - " -NoNewline -ForegroundColor Green
            Write-Host "Accessible" -ForegroundColor Gray
            $tcpClient.Close()
        }
    } catch {
        Write-Host "  ✗ localhost:$($portInfo.Port) - " -NoNewline -ForegroundColor Red
        Write-Host "Not accessible" -ForegroundColor Gray
    }
}

# Fix option
if ($Fix -and $isAdmin) {
    Write-Host ""
    Write-Host "Creating Missing Firewall Rules..." -ForegroundColor Yellow
    
    foreach ($portInfo in $requiredPorts) {
        $existingRule = Get-NetFirewallRule -DisplayName $portInfo.Name -ErrorAction SilentlyContinue
        if (-not $existingRule) {
            try {
                New-NetFirewallRule -DisplayName $portInfo.Name `
                                   -Description $portInfo.Description `
                                   -Direction Inbound `
                                   -Protocol TCP `
                                   -LocalPort $portInfo.Port `
                                   -Action Allow `
                                   -Profile Any `
                                   -Enabled True | Out-Null
                Write-Host "  ✓ Created rule for port $($portInfo.Port)" -ForegroundColor Green
            } catch {
                Write-Host "  ✗ Failed to create rule for port $($portInfo.Port): $_" -ForegroundColor Red
            }
        } else {
            Write-Host "  - Rule for $($portInfo.Name) already exists" -ForegroundColor Gray
        }
    }
}

# Summary and recommendations
Write-Host ""
Write-Host "Summary & Recommendations" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

if ($remoteCRules.Count -eq 0) {
    Write-Host "No RemoteC firewall rules found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "To fix this, run PowerShell as Administrator and execute:" -ForegroundColor Yellow
    Write-Host "  .\scripts\check-firewall.ps1 -Fix" -ForegroundColor White
    Write-Host ""
    Write-Host "Or manually create rules:" -ForegroundColor Yellow
    foreach ($portInfo in $requiredPorts) {
        Write-Host "  New-NetFirewallRule -DisplayName '$($portInfo.Name)' -Direction Inbound -Protocol TCP -LocalPort $($portInfo.Port) -Action Allow" -ForegroundColor Gray
    }
} else {
    Write-Host "Firewall rules are configured for RemoteC" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your laptop should be able to connect to:" -ForegroundColor Yellow
    foreach ($ip in $ipAddresses) {
        Write-Host "  - http://${ip}:7001 (API)" -ForegroundColor White
        Write-Host "  - http://${ip}:3000 (Web UI)" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "Additional Checks:" -ForegroundColor Yellow
Write-Host "  1. Ensure Docker containers are running: docker ps" -ForegroundColor Gray
Write-Host "  2. Check if laptop is on same network/subnet" -ForegroundColor Gray
Write-Host "  3. Try disabling Windows Defender Firewall temporarily for testing" -ForegroundColor Gray
Write-Host ""