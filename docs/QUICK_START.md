# RemoteC Quick Start Guide

This guide will help you get RemoteC up and running quickly in development mode.

## Prerequisites

1. **Development Tools**
   - Visual Studio 2022 or VS Code
   - .NET 8.0 SDK
   - Node.js 18+ and npm
   - SQL Server (local or remote)

2. **Remote SQL Server (provided)**
   - Server: `sqltest.schoolvision.net,14333`
   - Username: `sv`
   - Password: `Gv51076!`
   - Database: `RemoteCDb`

## Step 1: Clone and Setup

```batch
# Clone the repository
git clone [repository-url]
cd remotec

# Install web dependencies
cd src\RemoteC.Web
npm install
cd ..\..
```

## Step 2: Start the API Server

```batch
# From project root
scripts\start-server-windows.bat
```

The API will start on http://localhost:17001

## Step 3: Start the Web UI (Development Mode)

```batch
# From project root - No Azure AD required!
scripts\start-dev-ui.bat
```

The UI will start on http://localhost:3000

## Step 4: Login

1. Navigate to http://localhost:3000
2. You'll see a development login page
3. Enter any username/password (e.g., `admin`/`admin`)
4. Click "Sign in"

You're now logged in with full admin access!

## Step 5: Start a Host (Optional)

To test remote control functionality:

```batch
# From project root
scripts\start-host-windows.bat
```

## Development Features

- **No Azure AD Required**: Development mode bypasses Azure AD authentication
- **Instant Login**: Use any credentials to log in
- **Full Access**: All features available without configuration
- **Dev Badge**: UI shows "DEV" badge to indicate development mode
- **Hot Reload**: Changes to React code reload automatically

## Troubleshooting

### "vite is not recognized"
```batch
cd src\RemoteC.Web
npm install
```

### Port already in use
- API uses port 17001
- UI uses port 3000
- Stop any processes using these ports

### Database connection issues
The remote SQL server is pre-configured. If you have issues:
1. Check your internet connection
2. Verify firewall allows outbound connections to port 14333

### Azure AD errors
If you see Azure AD errors, you're not in development mode. Use:
```batch
scripts\start-dev-ui.bat
```

## Next Steps

1. **Explore the UI**: Browse devices, sessions, and settings
2. **Test Remote Control**: Connect a host and client
3. **Check the API**: Visit http://localhost:17001/swagger
4. **Modify Code**: Changes auto-reload in development

## Switching to Production Mode

To use Azure AD authentication:
1. Configure Azure AD B2C settings in `appsettings.json`
2. Build for production: `npm run build`
3. Set `NODE_ENV=production`

Happy coding! 🚀