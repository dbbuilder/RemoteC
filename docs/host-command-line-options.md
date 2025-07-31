# RemoteC Host Command Line Options

The RemoteC Host application now supports command line configuration to override settings in appsettings.json.

## Available Options

| Option | Short Form | Description | Example |
|--------|------------|-------------|---------|
| `--server` | `-s` | API server base URL | `--server http://10.0.0.91:17001` |
| `--host-id` | `--id` | Host identifier | `--id dev-host-001` |
| `--host-secret` | `--secret` | Host authentication secret | `--secret dev-secret-001` |
| `--token-endpoint` | | Full token endpoint URL | `--token-endpoint http://10.0.0.91:17001/api/auth/host/token` |

## Usage Examples

### Basic Usage
```bash
# Run with custom server URL
dotnet run -- --server http://10.0.0.91:17001 --id dev-host-001 --secret dev-secret-001

# Run with full token endpoint specification
dotnet run -- --server http://10.0.0.91:17001 --id dev-host-001 --secret dev-secret-001 --token-endpoint http://10.0.0.91:17001/api/auth/host/token
```

### Using Scripts

#### Windows Batch Script
```cmd
# Default configuration (localhost)
scripts\start-host-with-params.bat

# Custom server
scripts\start-host-with-params.bat http://10.0.0.91:17001

# Custom server with different credentials
scripts\start-host-with-params.bat http://10.0.0.91:17001 my-host-id my-host-secret
```

#### PowerShell Script
```powershell
# Default configuration
.\scripts\start-host-with-params.ps1

# Custom server
.\scripts\start-host-with-params.ps1 -ServerUrl http://10.0.0.91:17001

# Full customization
.\scripts\start-host-with-params.ps1 `
    -ServerUrl http://10.0.0.91:17001 `
    -HostId "prod-host-001" `
    -HostSecret "prod-secret" `
    -TokenEndpoint "http://10.0.0.91:17001/api/auth/host/token"
```

### For Remote Connections

When connecting from a remote machine to a server on a different network:

```bash
# From remote machine to server at 10.0.0.91
dotnet run -- --server http://10.0.0.91:17001 --id dev-host-001 --secret dev-secret-001
```

## Configuration Precedence

Settings are applied in the following order (later sources override earlier ones):
1. appsettings.json
2. appsettings.{Environment}.json
3. Environment variables
4. Command line arguments

## Important Notes

- The `--token-endpoint` parameter is optional. If not specified, it will be constructed as `{server}/api/auth/host/token`
- Ensure the host ID and secret match what's configured on the server
- For production use, consider using environment variables or secure configuration management instead of command line arguments for secrets