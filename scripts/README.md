# RemoteC Scripts

This directory contains various scripts to help with development, deployment, and testing of RemoteC.

## Quick Start Scripts

### For Development

- **`start-dev-server.bat`** / **`start-dev-server.ps1`**
  - Starts the server in development mode with SQLite
  - No external dependencies required (no SQL Server, Redis, or Hangfire)
  - Creates development configuration if missing
  - Server runs on http://localhost:17001

- **`test-server-startup.bat`**
  - Tests if the server can start successfully
  - Checks the health endpoint
  - Shows server logs for debugging

### For Deployment

- **`deploy-host.ps1`**
  - Deploys the RemoteC host application
  - Configures Windows service
  - Sets up auto-start on boot

- **`deploy-client.ps1`**
  - Deploys the RemoteC client application
  - Creates desktop shortcuts
  - Configures server connection

- **`quick-deploy.ps1`**
  - One-click deployment script
  - Deploys both server and host components
  - Configures all necessary settings

### Utility Scripts

- **`check-dependencies.ps1`**
  - Verifies all prerequisites are installed
  - Checks .NET SDK, SQL Server, Redis
  - Reports missing components

- **`create-dev-certs.ps1`**
  - Creates self-signed certificates for HTTPS
  - Trusts certificates in local store
  - Required for HTTPS in development

- **`test-deployment.ps1`**
  - Runs comprehensive deployment tests
  - Verifies all endpoints are accessible
  - Checks database connectivity

## Usage Examples

### Start Development Server
```bash
# Using Command Prompt
scripts\start-dev-server.bat

# Using PowerShell
.\scripts\start-dev-server.ps1
```

### Deploy to Production
```bash
# Full deployment
.\scripts\quick-deploy.ps1 -Environment Production -ServerUrl https://remotec.company.com

# Deploy only host
.\scripts\deploy-host.ps1 -ServerUrl https://remotec.company.com
```

### Test Deployment
```bash
# Run all tests
.\scripts\test-deployment.ps1 -ServerUrl http://localhost:17001

# Quick health check
.\scripts\test-server-startup.bat
```

## Script Parameters

Most PowerShell scripts support common parameters:

- `-Environment` - Target environment (Development, Staging, Production)
- `-ServerUrl` - RemoteC server URL
- `-InstallPath` - Installation directory
- `-ServiceName` - Windows service name (for host deployment)
- `-Verbose` - Enable detailed logging

## Troubleshooting

If scripts fail to run:

1. **Execution Policy** - Run PowerShell as Administrator and execute:
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

2. **Missing Dependencies** - Run the dependency check:
   ```powershell
   .\scripts\check-dependencies.ps1
   ```

3. **Port Conflicts** - The default port 17001 may be in use. Check with:
   ```bash
   netstat -an | findstr 17001
   ```

4. **Permission Issues** - Some scripts require administrator privileges. Right-click and "Run as Administrator".

## Development Tips

- Use `start-dev-server.*` scripts for local development
- The development server uses SQLite, so no database setup is required
- All external dependencies (Redis, Hangfire, Azure Key Vault) are disabled in development mode
- Check the server logs in `src\RemoteC.Api\logs` for debugging