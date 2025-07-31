# RemoteC Host Connection Guide

## Overview
The RemoteC Host is the application that runs on machines you want to control remotely. It connects to the RemoteC server and enables remote access.

## Quick Start

### Scenario 1: Host on Same Machine as Server
If you're testing on the same machine:
```batch
scripts\start-host-windows.bat
# Choose option 1 (localhost)
```

### Scenario 2: Host on Different Machine
If the host is on a different machine from the server:

1. **On the Server Machine (10.0.0.91)**:
   ```batch
   # Ensure server is running
   scripts\start-server-windows.bat
   
   # Ensure firewall allows port 17001
   # Check Windows Defender Firewall settings
   ```

2. **On the Host Machine**:
   ```batch
   scripts\start-host-remote.bat
   # Enter server IP: 10.0.0.91
   # Enter host name: (or press Enter for computer name)
   ```

## Manual Host Configuration

If the scripts don't work, you can manually configure:

### Option 1: From Source Code
```batch
cd src\RemoteC.Host
set REMOTEC_SERVER_URL=http://10.0.0.91:17001
set REMOTEC_HOST_ID=MyHostPC
set REMOTEC_HOST_SECRET=dev-secret-001
dotnet run
```

### Option 2: Using Command Line Arguments
```batch
cd src\RemoteC.Host
dotnet run -- --server http://10.0.0.91:17001 --id MyHostPC --secret dev-secret-001
```

## Connection Requirements

1. **Network Connectivity**:
   - Host must be able to reach server on port 17001
   - Test with: `ping 10.0.0.91`
   - Test API: `curl http://10.0.0.91:17001/health`

2. **Firewall Configuration**:
   - Server machine must allow incoming connections on port 17001
   - Host machine must allow outgoing connections

3. **Authentication**:
   - Development mode uses fixed secret: `dev-secret-001`
   - Production mode will use proper authentication

## Troubleshooting

### "Cannot connect to server"
1. Check server is running: `netstat -an | findstr 17001`
2. Check firewall on server machine
3. Try accessing from browser: `http://10.0.0.91:17001/health`

### "Unauthorized"
- Ensure using correct secret: `dev-secret-001`
- Check server logs for authentication errors

### "Host already registered"
- Each host needs a unique ID
- Use different name or restart server to clear registrations

## What Happens After Connection

1. Host registers with server
2. Host appears in Devices list in UI
3. Users can initiate remote sessions
4. Host receives commands via SignalR connection
5. Screen capture and control are enabled

## Security Notes

- In development mode, authentication is simplified
- Always use HTTPS in production
- Consider VPN for sensitive environments
- Host runs with user privileges of the account that started it

## Next Steps

After host connects:
1. Open RemoteC UI: http://10.0.0.91:17002
2. Navigate to Devices to see connected host
3. Create a new session with the host
4. Start remote control

## Architecture

```
[Host Machine] <-- SignalR WebSocket --> [Server:17001] <-- HTTP --> [Web UI:17002]
     |                                          |
     |                                          |
  Screen Capture                            Database
  Mouse/Keyboard                              Redis
  File Transfer                              Sessions
```