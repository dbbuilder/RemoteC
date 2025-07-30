# RemoteC UI Deployment Guide

## Architecture Overview

```
┌─────────────────────────────────┐
│   Server Machine (10.0.0.91)    │
├─────────────────────────────────┤
│  RemoteC.Api (Port 17001)       │ ← Backend API
│  RemoteC.Web (Port 3000)        │ ← React Web UI
│  SQL Server Database            │ ← Data Storage
└─────────────────────────────────┘
           ↑               ↑
           │               │
    API Calls         Web Browser
           │               │
┌──────────┴──────┐ ┌──────┴──────┐
│  Host Machine 1 │ │ Any Machine │
│  RemoteC.Host   │ │ Web Browser │
└─────────────────┘ └─────────────┘
```

## Starting the UI

The UI runs on the **server machine** alongside the API.

### Option 1: Start Everything at Once (Recommended)

```batch
D:\dev2\remotec\scripts\start-all-windows.bat
```

This starts both:
- API Server on port 17001
- Web UI on port 3000

### Option 2: Start UI Separately

If the API is already running:
```batch
D:\dev2\remotec\scripts\start-ui-windows.bat
```

Or manually:
```batch
cd D:\dev2\remotec\src\RemoteC.Web
npm install  # First time only
npm run dev
```

## Accessing the UI

Once running, the UI is accessible at:

- **From Server Machine**: http://localhost:3000
- **From Network**: http://10.0.0.91:3000

## What You'll See

1. **Login Page** - Default authentication in development mode
2. **Dashboard** - Overview of connected devices and active sessions
3. **Devices** - List of registered host machines
4. **Sessions** - Active and past remote control sessions
5. **Users** - User management (admin only)
6. **Settings** - Configuration options

## UI Features

### Main Pages:
- **Dashboard**: Real-time overview of system status
- **Devices**: View and manage connected hosts
- **Sessions**: Start and monitor remote control sessions
- **Audit Logs**: Security and compliance tracking

### Key Functions:
1. **Start Remote Session**: Select a device and initiate control
2. **View Screen**: Real-time screen sharing
3. **File Transfer**: Upload/download files to/from remote hosts
4. **Session Recording**: Record sessions for compliance

## Network Requirements

For the UI to work properly:

1. **Port 3000** must be open for web browser access
2. **Port 17001** must be accessible (API backend)
3. **WebSocket support** for real-time features (SignalR)

## Troubleshooting

### UI Won't Start
```batch
# Check Node.js is installed
node --version

# Clear cache and reinstall
cd D:\dev2\remotec\src\RemoteC.Web
rmdir /s /q node_modules
npm install
```

### Can't Connect from Network
1. Check Windows Firewall allows port 3000
2. Verify server is listening on 0.0.0.0 (not just localhost)
3. Test with: `http://10.0.0.91:3000`

### API Connection Issues
- Check API is running: `http://10.0.0.91:17001/health`
- Verify proxy settings in vite.config.ts
- Check browser console for errors (F12)

## Development vs Production

Currently configured for **development**:
- No HTTPS required
- Simplified authentication
- Hot module reloading
- Detailed error messages

For production deployment:
- Build static files: `npm run build`
- Serve with a web server (IIS, nginx)
- Enable HTTPS
- Configure proper authentication

## Quick Test

1. Start the full stack:
   ```batch
   D:\dev2\remotec\scripts\start-all-windows.bat
   ```

2. Open browser to: http://10.0.0.91:3000

3. You should see the RemoteC login page

4. In development mode, any username/password works