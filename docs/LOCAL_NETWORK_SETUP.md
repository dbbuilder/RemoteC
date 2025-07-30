# RemoteC Local Network Setup Guide

This guide explains how to set up RemoteC server and host across machines on a local network.

## Prerequisites

- Two Windows machines on the same local network
- .NET 8.0 SDK installed on both machines
- Windows Firewall configured to allow traffic on port 17001
- SQL Server accessible from the server machine

## Server Setup (Machine 1)

### 1. Get Server IP Address
```powershell
# On the server machine, find your local IP
ipconfig
# Look for IPv4 Address (e.g., 192.168.1.100)
```

### 2. Configure Firewall
```powershell
# Run as Administrator to open port 17001
New-NetFirewallRule -DisplayName "RemoteC API" -Direction Inbound -Protocol TCP -LocalPort 17001 -Action Allow
```

### 3. Start the Server
```bash
# Navigate to API directory
cd src/RemoteC.Api

# Run the server (it will bind to all interfaces on port 17001)
dotnet run --urls "http://0.0.0.0:17001"

# Or use the provided script
../../scripts/start-server.bat
```

### 4. Verify Server is Accessible
```powershell
# From the server machine
curl http://localhost:17001/health

# From another machine on the network (replace with your server IP)
curl http://192.168.1.100:17001/health
```

## Host Setup (Machine 2)

### 1. Configure Host Application
```powershell
# Navigate to host directory
cd src/RemoteC.Host

# Edit appsettings.json to point to server
# Replace "localhost" with your server's IP address
```

Update `appsettings.json`:
```json
{
  "RemoteControl": {
    "ServerUrl": "http://192.168.1.100:17001",  // Replace with your server IP
    "DeviceId": "auto",
    "DeviceName": "auto",
    "AllowedViewers": [],
    "RequireConsent": true
  }
}
```

### 2. Start the Host
```powershell
# Build and run the host
dotnet build
dotnet run

# Or use the provided script
../../scripts/start-host.bat
```

## Quick Setup Scripts

### Server Start Script (`start-server-network.bat`)
```batch
@echo off
echo Starting RemoteC Server on all network interfaces...
cd /d "%~dp0\..\src\RemoteC.Api"

echo.
echo Server will be accessible at:
echo   - http://localhost:17001
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /C:"IPv4"') do (
    echo   - http://%%a:17001
)
echo.

dotnet run --urls "http://0.0.0.0:17001"
```

### Host Start Script (`start-host-network.bat`)
```batch
@echo off
echo Starting RemoteC Host...
cd /d "%~dp0\..\src\RemoteC.Host"

set /p SERVER_IP="Enter server IP address (e.g., 192.168.1.100): "
echo.
echo Connecting to server at http://%SERVER_IP%:17001
echo.

set RemoteControl__ServerUrl=http://%SERVER_IP%:17001
dotnet run
```

## Troubleshooting

### Server Not Accessible from Network

1. **Check Windows Firewall**
   ```powershell
   # List firewall rules
   Get-NetFirewallRule -DisplayName "*RemoteC*"
   
   # Test port connectivity from client
   Test-NetConnection -ComputerName 192.168.1.100 -Port 17001
   ```

2. **Check Server is Listening on All Interfaces**
   ```powershell
   # On server machine
   netstat -an | findstr :17001
   # Should show 0.0.0.0:17001 LISTENING
   ```

3. **Verify Network Connectivity**
   ```powershell
   # From host machine, ping server
   ping 192.168.1.100
   ```

### Host Cannot Connect to Server

1. **Check Server URL Configuration**
   - Ensure `appsettings.json` has correct server IP
   - No trailing slashes in URL
   - Using http:// not https:// for local testing

2. **Check Authentication**
   - In development mode, authentication is simplified
   - Ensure both machines are in Development environment

3. **Check Server Logs**
   - Look for connection attempts in server console
   - Check for any authentication or CORS errors

## Security Considerations

For local network testing:
- Server runs in Development mode with simplified authentication
- CORS is configured to allow the React app
- Consider using HTTPS for production deployments
- Implement proper authentication for production use

## Network Architecture

```
┌─────────────────┐         ┌─────────────────┐
│   Server PC     │         │    Host PC      │
│                 │         │                 │
│  RemoteC.Api    │◄────────┤  RemoteC.Host  │
│  Port: 17001    │  HTTP   │                 │
│                 │         │                 │
│  SQL Server     │         │  Screen Capture │
│  (Remote/Local) │         │  Input Control  │
└─────────────────┘         └─────────────────┘
        │                           │
        │                           │
        ▼                           ▼
   Web Browser                 Remote Control
   (Optional)                    Session
```

## Next Steps

1. Test basic connectivity between server and host
2. Register the host device with the server
3. Create a remote control session
4. Test screen sharing and input control
5. Configure security settings for production use

## Additional Resources

- [RemoteC Architecture](ARCHITECTURE.md)
- [API Documentation](../src/RemoteC.Api/README.md)
- [Host Configuration](../src/RemoteC.Host/README.md)