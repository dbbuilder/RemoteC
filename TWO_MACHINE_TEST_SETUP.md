# Two-Machine RemoteC Test Setup Guide

This is a simplified guide to quickly test RemoteC between two Windows machines.

## Quick Setup (10 minutes)

### Prerequisites
- Two Windows 10/11 machines on the same network
- Administrator access on both machines
- .NET 8.0 Runtime installed ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))

### Machine 1: Server Setup

1. **Download and Extract RemoteC**
   ```powershell
   # Run PowerShell as Administrator
   cd C:\
   git clone https://github.com/dbbuilder/RemoteC.git
   cd RemoteC
   ```

2. **Run Quick Deploy Script**
   ```powershell
   # Allow script execution
   Set-ExecutionPolicy -ExecutionPolicy RemoteSe -Scope CurrentUser -Force
   
   # Deploy server components
   .\scripts\deploy-quick-start.ps1 -Mode Server
   ```

3. **Note Your Server IP**
   The script will display your IP addresses. Note the one on your local network (e.g., 192.168.1.100)

### Machine 2: Host Setup

1. **Download and Extract RemoteC**
   ```powershell
   # Run PowerShell as Administrator
   cd C:\
   git clone https://github.com/dbbuilder/RemoteC.git
   cd RemoteC
   ```

2. **Run Host Deploy Script**
   ```powershell
   # Replace SERVER_IP with Machine 1's IP
   .\scripts\deploy-quick-start.ps1 -Mode Host -ServerIP 192.168.1.100
   ```

## Testing the Connection

### On Machine 1 (Server):

1. **Start the Server**
   ```powershell
   cd C:\RemoteC-Server
   .\start-server.bat
   ```

2. **Verify Server is Running**
   - Open browser: http://localhost:7001
   - You should see the RemoteC interface

### On Machine 2 (Host):

1. **Start the Host**
   ```powershell
   cd C:\RemoteC-Host
   .\start-host.bat
   ```

2. **Note the PIN** (if displayed)
   - A 6-digit PIN will appear for secure connection

### Connect and Test:

1. **From Any Browser** (can be Machine 1, 2, or another computer):
   - Navigate to: http://MACHINE1_IP:7001
   - You'll see the RemoteC dashboard

2. **View Available Machines**
   - Machine 2 should appear as "Online"
   - Click "Connect" next to Machine 2

3. **Enter PIN** (if required)
   - Enter the 6-digit PIN from Machine 2

4. **Test Remote Control**
   - You should now see Machine 2's screen
   - Test mouse movement and clicks
   - Test keyboard input
   - Try file transfer (drag & drop)

## Quick Troubleshooting

### Can't see Machine 2 in dashboard?
```powershell
# On Machine 2, test connection to server
.\scripts\test-remote-connection.ps1 -ServerIP 192.168.1.100
```

### Connection refused errors?
```powershell
# On Machine 1, check firewall
netsh advfirewall firewall add rule name="RemoteC" dir=in action=allow protocol=TCP localport=7001,7002
```

### Performance issues?
- Check both machines are on same network/subnet
- Verify no VPN is interfering
- Check CPU usage on both machines

## Simple Test Checklist

- [ ] Server running on Machine 1
- [ ] Can access http://MACHINE1_IP:7001 from browser
- [ ] Host running on Machine 2
- [ ] Machine 2 appears as "Online" in dashboard
- [ ] Can connect to Machine 2
- [ ] Can see Machine 2's screen
- [ ] Mouse control works
- [ ] Keyboard input works
- [ ] Can transfer files
- [ ] Latency is acceptable (<100ms)

## Next Steps

If basic testing works:
1. Configure SSL/HTTPS for security
2. Set up authentication with Azure AD B2C
3. Test advanced features (multi-monitor, clipboard sync)
4. Deploy in production environment

## Common Commands

**Check if services are running:**
```powershell
# Machine 1
netstat -an | findstr :7001

# Machine 2
Get-Process | Where-Object {$_.Name -like "*RemoteC*"}
```

**View logs:**
```powershell
# Machine 1
Get-Content C:\RemoteC-Server\logs\api.log -Tail 50

# Machine 2
Get-Content C:\RemoteC-Host\logs\host.log -Tail 50
```

**Stop services:**
```powershell
# Machine 1
cd C:\RemoteC-Server
docker-compose down

# Machine 2
Stop-Process -Name "RemoteC.Host" -Force
```

That's it! You should now have RemoteC running between two machines for testing.