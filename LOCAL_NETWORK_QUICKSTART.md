# RemoteC Local Network Quick Start Guide

Based on your network configuration where the server machine has IP: **10.0.0.91**

## Server Setup (on machine with IP 10.0.0.91)

### 1. Configure Windows Firewall
Run PowerShell as Administrator:
```powershell
.\scripts\configure-firewall.ps1
```

### 2. Start the Server
Run in Windows (NOT in WSL):

**Option 1: Use the Windows batch script**
```batch
D:\dev2\remotec\scripts\start-server-windows.bat
```

**Option 2: Manual start in Windows PowerShell**
```powershell
cd D:\dev2\remotec\src\RemoteC.Api
dotnet run --urls "http://0.0.0.0:17001"
```

The server will be accessible at:
- From WSL/localhost: `http://localhost:17001`
- From network: `http://10.0.0.91:17001`

### 3. Verify Server is Running
From the server machine:
```powershell
curl http://localhost:17001/health
```

From another machine on the network:
```powershell
curl http://10.0.0.91:17001/health
```

## Host Setup (on client machines)

### 1. Configure Host
Edit `src/RemoteC.Host/appsettings.Development.json`:
```json
{
  "Api": {
    "BaseUrl": "http://10.0.0.91:17001",
    "TokenEndpoint": "http://10.0.0.91:17001/api/auth/token"
  },
  "HostConfiguration": {
    "ServerUrl": "http://10.0.0.91:17001"
  }
}
```

### 2. Start the Host
```powershell
cd src\RemoteC.Host
dotnet run
```

## Quick Test Commands

Test connectivity from any machine:
```powershell
# Test network connectivity
ping 10.0.0.91

# Test port connectivity
Test-NetConnection -ComputerName 10.0.0.91 -Port 17001

# Test API health
Invoke-RestMethod -Uri "http://10.0.0.91:17001/health"
```

## Important: Run Server in Windows

**DO NOT run the server from WSL** for network access. Always run it in Windows:
1. Open Windows PowerShell or Command Prompt (not WSL)
2. Navigate to `D:\dev2\remotec\src\RemoteC.Api`
3. Run the server with `dotnet run --urls "http://0.0.0.0:17001"`

This ensures the server is accessible from other machines on your network at `http://10.0.0.91:17001`

## Troubleshooting

If the server isn't accessible from the network:

1. **Check Windows Defender Firewall**
   ```powershell
   # Run as Administrator
   Get-NetFirewallRule -DisplayName "*RemoteC*"
   
   # If no rule exists, create it:
   New-NetFirewallRule -DisplayName "RemoteC API" -Direction Inbound -Protocol TCP -LocalPort 17001 -Action Allow
   ```

2. **Verify Server is Listening**
   ```powershell
   # Should show 0.0.0.0:17001 in LISTENING state
   netstat -an | findstr :17001
   ```

3. **Test from Another Machine**
   ```powershell
   # Basic connectivity test
   ping 10.0.0.91
   
   # Port test
   Test-NetConnection -ComputerName 10.0.0.91 -Port 17001
   ```