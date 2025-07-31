# RemoteC Scripts Guide

This directory contains essential scripts for building and running RemoteC.

## Quick Start

### Development Mode (No Azure AD Required)
```batch
# 1. Install dependencies (first time only)
install-web-dependencies.bat

# 2. Start API server
start-server-windows.bat

# 3. Start UI in development mode
start-dev-ui.bat
# Login with any username/password (e.g., admin/admin)

# 4. (Optional) Start host for testing
start-host-windows.bat
```

### Production Mode (Azure AD Required)
```batch
# 1. Configure Azure AD in appsettings.json

# 2. Start API server
start-server-windows.bat

# 3. Start UI in production mode
start-ui-production.bat
# Login with Azure AD credentials
```

## Essential Scripts

### Build Scripts
- `build.bat` - Build entire solution (Windows)
- `build.sh` - Build entire solution (Linux/WSL)

### Server Scripts
- `start-server-windows.bat` - Start API server on port 17001

### UI Scripts
- `start-dev-ui.bat` - Start UI in development mode (no Azure AD)
- `start-ui-production.bat` - Start UI in production mode (Azure AD required)
- `install-web-dependencies.bat` - Install npm dependencies

### Host Scripts
- `start-host-windows.bat` - Start host application for remote control

### Utility Scripts
- `check-and-kill-port.bat` - Kill process using a specific port
- `fix-web-dependencies.bat` - Fix npm dependency issues

### Test Scripts
- `run-tests-windows.ps1` - Run all tests
- `run-specific-test-windows.ps1` - Run specific test

### Deployment Scripts
- `deploy-docker.sh` - Deploy with Docker
- `deploy-k8s.sh` - Deploy to Kubernetes

## Switching Between Development and Production

### Environment Detection
The UI automatically detects the environment:
- `npm run dev` → Development mode (no Azure AD)
- `npm run build && npm run preview` → Production mode (Azure AD required)

### Manual Override
Set environment variable to force mode:
```batch
# Force development mode
set NODE_ENV=development
npm run dev

# Force production mode
set NODE_ENV=production
npm run build
npm run preview
```

### Key Differences

| Feature | Development Mode | Production Mode |
|---------|-----------------|-----------------|
| Authentication | Any username/password | Azure AD B2C |
| UI Badge | Shows "DEV" | No badge |
| Security | Relaxed for testing | Full RBAC |
| API Access | Dev token | Azure AD token |
| Hot Reload | Yes | No |

## Troubleshooting

### Port Already in Use
```batch
check-and-kill-port.bat 17001  # Kill API server
check-and-kill-port.bat 17002  # Kill UI server
```

### NPM Dependencies Issues
```batch
fix-web-dependencies.bat
```

### Can't Connect to SQL Server
Check connection string in `appsettings.Development.json`:
- Server: `sqltest.schoolvision.net,14333`
- Database: `RemoteCDb`
- Credentials provided in config

## Best Practices

1. **Always use the appropriate mode**:
   - Development for local testing
   - Production for deployments

2. **Don't commit Azure AD credentials**:
   - Use environment variables or Azure Key Vault

3. **Test both modes**:
   - Ensure features work in both authentication modes

4. **Use provided scripts**:
   - Don't create custom variations
   - Report issues if scripts need updates