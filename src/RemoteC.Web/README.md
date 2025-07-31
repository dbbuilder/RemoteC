# RemoteC Web UI

This is the React-based web interface for RemoteC.

## Prerequisites

- Node.js 18+ and npm (Download from https://nodejs.org/)
- The RemoteC API server running on port 17001

## Quick Start

### Development Mode (No Azure AD Required)

In development mode, the UI bypasses Azure AD authentication:

```batch
# From project root - starts UI with dev authentication
scripts\start-dev-ui.bat

# Login with any username/password (e.g., admin/admin)
```

### Production Mode (Azure AD Required)

```batch
# Install dependencies (first time only)
scripts\install-web-dependencies.bat

# Start the UI
scripts\start-ui-windows.bat
```

### Manual Setup

```batch
# Navigate to the web directory
cd D:\dev2\remotec\src\RemoteC.Web

# Install dependencies
npm install

# Start the development server
npm run dev
```

## Authentication Modes

### Development Mode
- Automatically enabled when running `npm run dev`
- No Azure AD configuration required
- Login with any username/password
- Full admin access granted
- Shows "DEV" badge in the UI

### Production Mode
- Requires Azure AD B2C configuration
- Used when building for production
- Full RBAC support

## Common Issues

### Azure AD Authentication Errors

If you see "endpoints_resolution_error", you're likely missing Azure AD configuration. Use development mode instead:

```batch
scripts\start-dev-ui.bat
```

### "vite is not recognized as an internal or external command"

This means dependencies aren't installed. Run:
```batch
npm install
```

### "Cannot find module" errors

Delete node_modules and reinstall:
```batch
rmdir /s /q node_modules
del package-lock.json
npm install
```

### Port 3000 already in use

Either:
1. Stop the process using port 3000
2. Change the port in vite.config.ts

## Access URLs

- Local: http://localhost:3000
- Network: http://YOUR_IP:3000 (e.g., http://10.0.0.91:3000)

## Development

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run lint` - Run ESLint
- `npm test` - Run tests

## Architecture

The UI uses:
- React 18 with TypeScript
- Vite as the build tool
- Tailwind CSS for styling
- Radix UI for components
- SignalR for real-time communication
- React Query for data fetching

## Configuration

The UI connects to the API server configured in `vite.config.ts`. By default:
- API: http://localhost:17001/api
- SignalR Hubs: http://localhost:17001/hubs