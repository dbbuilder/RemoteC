# RemoteC Deployment and Testing Guide

This guide walks you through deploying RemoteC and testing it between two machines.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Deployment Options](#deployment-options)
3. [Option A: Docker Deployment](#option-a-docker-deployment)
4. [Option B: Manual Deployment](#option-b-manual-deployment)
5. [Testing Between Two Machines](#testing-between-two-machines)
6. [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software
- **Windows 10/11** or **Windows Server 2019+** (for Host machine)
- **.NET 8.0 Runtime** (for manual deployment)
- **Docker Desktop** (for Docker deployment)
- **SQL Server 2019+** (or use Docker container)
- **Redis** (or use Docker container)

### Network Requirements
- Machines must be on the same network or have connectivity
- Open ports: 
  - **7001**: API Server
  - **7002**: SignalR Hub
  - **1433**: SQL Server
  - **6379**: Redis

## Deployment Options

You can deploy RemoteC using either Docker (recommended) or manually.

## Option A: Docker Deployment

### 1. Deploy Infrastructure (Machine 1 - Server)

```bash
# Clone the repository
git clone https://github.com/dbbuilder/RemoteC.git
cd RemoteC

# Create a docker network
docker network create remotec-network

# Start SQL Server
docker run -d \
  --name remotec-sqlserver \
  --network remotec-network \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=YourStrong@Password123" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2019-latest

# Start Redis
docker run -d \
  --name remotec-redis \
  --network remotec-network \
  -p 6379:6379 \
  redis:7-alpine

# Wait for SQL Server to start (30 seconds)
sleep 30

# Initialize the database
docker exec -i remotec-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Password123" \
  -Q "CREATE DATABASE RemoteC2Db"
```

### 2. Configure and Start the API Server

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  remotec-api:
    build:
      context: .
      dockerfile: src/RemoteC.Api/Dockerfile
    container_name: remotec-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:7001
      - ConnectionStrings__DefaultConnection=Server=remotec-sqlserver;Database=RemoteC2Db;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=true
      - ConnectionStrings__Redis=remotec-redis:6379
      - AzureAdB2C__Instance=https://yourtenant.b2clogin.com
      - AzureAdB2C__ClientId=your-client-id
      - AzureAdB2C__Domain=yourtenant.onmicrosoft.com
      - AzureAdB2C__SignUpSignInPolicyId=B2C_1_SignUpSignIn
    ports:
      - "7001:7001"
      - "7002:7002"
    networks:
      - remotec-network
    depends_on:
      - remotec-sqlserver
      - remotec-redis

  remotec-host:
    build:
      context: .
      dockerfile: src/RemoteC.Host/Dockerfile
    container_name: remotec-host
    environment:
      - ApiUrl=http://remotec-api:7001
      - SignalRUrl=http://remotec-api:7002/hubs/session
    networks:
      - remotec-network
    depends_on:
      - remotec-api
    volumes:
      - /tmp/.X11-unix:/tmp/.X11-unix:rw
    environment:
      - DISPLAY=${DISPLAY}

networks:
  remotec-network:
    external: true
```

Start the services:

```bash
# Build and start all services
docker-compose up -d

# Check logs
docker-compose logs -f
```

### 3. Deploy Host Application (Machine 2 - Host)

On the machine you want to control:

```bash
# Pull and run the Host application
docker run -d \
  --name remotec-host \
  -e ApiUrl=http://MACHINE1_IP:7001 \
  -e SignalRUrl=http://MACHINE1_IP:7002/hubs/session \
  -e MachineName=MyComputer \
  --network host \
  remotec/host:1.0.0
```

## Option B: Manual Deployment

### 1. Set Up Infrastructure (Machine 1 - Server)

#### Install SQL Server
1. Download and install SQL Server 2019 Developer Edition
2. Create a database named `RemoteC2Db`
3. Run the initialization script:

```sql
-- Create database
CREATE DATABASE RemoteC2Db;
GO

USE RemoteC2Db;
GO

-- Run all scripts in database/scripts folder
-- (Tables, stored procedures, etc.)
```

#### Install Redis
```powershell
# Using Chocolatey
choco install redis-64

# Or download from https://github.com/tporadowski/redis/releases
```

### 2. Deploy API Server (Machine 1)

```powershell
# Build the API
cd D:\RemoteC
dotnet publish src\RemoteC.Api\RemoteC.Api.csproj -c Release -o D:\RemoteC-Deploy\Api

# Configure appsettings.json
$config = @{
    ConnectionStrings = @{
        DefaultConnection = "Server=localhost;Database=RemoteC2Db;Integrated Security=true;TrustServerCertificate=true"
        Redis = "localhost:6379"
    }
    AzureAdB2C = @{
        Instance = "https://yourtenant.b2clogin.com"
        ClientId = "your-client-id"
        Domain = "yourtenant.onmicrosoft.com"
        SignUpSignInPolicyId = "B2C_1_SignUpSignIn"
    }
}
$config | ConvertTo-Json -Depth 10 | Set-Content D:\RemoteC-Deploy\Api\appsettings.Production.json

# Run as Windows Service
sc.exe create RemoteCApi binPath="D:\RemoteC-Deploy\Api\RemoteC.Api.exe" start=auto
sc.exe start RemoteCApi
```

### 3. Deploy Host Application (Machine 2)

```powershell
# Build the Host
cd D:\RemoteC
dotnet publish src\RemoteC.Host\RemoteC.Host.csproj -c Release -o D:\RemoteC-Deploy\Host

# Configure appsettings.json
$config = @{
    ApiSettings = @{
        ApiUrl = "http://MACHINE1_IP:7001"
        SignalRUrl = "http://MACHINE1_IP:7002/hubs/session"
    }
    HostSettings = @{
        MachineName = $env:COMPUTERNAME
        AutoStart = $true
    }
}
$config | ConvertTo-Json -Depth 10 | Set-Content D:\RemoteC-Deploy\Host\appsettings.json

# Run as Windows Service
sc.exe create RemoteCHost binPath="D:\RemoteC-Deploy\Host\RemoteC.Host.exe" start=auto
sc.exe start RemoteCHost
```

## Testing Between Two Machines

### 1. Verify Services are Running

On Machine 1 (Server):
```powershell
# Check API is running
Invoke-WebRequest http://localhost:7001/health -UseBasicParsing

# Check SignalR hub
Invoke-WebRequest http://localhost:7002/hubs/session -UseBasicParsing
```

On Machine 2 (Host):
```powershell
# Check Host service
Get-Service RemoteCHost
```

### 2. Access the Web Interface

1. Open a browser on any machine
2. Navigate to `http://MACHINE1_IP:7001`
3. You should see the RemoteC login page

### 3. Register and Login

1. Click "Sign Up" to create a new account
2. Complete the Azure AD B2C registration
3. Login with your credentials

### 4. Connect to Remote Machine

1. In the dashboard, you should see Machine 2 listed as "Online"
2. Click "Connect" to initiate a remote session
3. Enter the PIN displayed on Machine 2 (if PIN authentication is enabled)

### 5. Test Remote Control Features

Test the following features:
- **Screen Viewing**: You should see Machine 2's screen
- **Mouse Control**: Click and move mouse on the remote screen
- **Keyboard Input**: Type in the remote session
- **File Transfer**: Drag and drop files between machines
- **Clipboard Sync**: Copy/paste between machines

### 6. Performance Verification

Monitor the following metrics:
- **Latency**: Should be <100ms on LAN
- **Frame Rate**: Should maintain 30+ FPS
- **CPU Usage**: Should stay below 50%

## Testing Commands

### Quick Connection Test
```powershell
# From Machine 2, test connection to API
Test-NetConnection -ComputerName MACHINE1_IP -Port 7001
Test-NetConnection -ComputerName MACHINE1_IP -Port 7002

# Test API endpoint
Invoke-RestMethod -Uri "http://MACHINE1_IP:7001/api/devices" -Method Get
```

### Monitor Logs

Docker:
```bash
# API logs
docker logs remotec-api -f

# Host logs
docker logs remotec-host -f
```

Manual deployment:
```powershell
# Windows Event Viewer
eventvwr.msc
# Navigate to Applications and Services Logs > RemoteC
```

## Troubleshooting

### Common Issues

1. **Cannot connect to API**
   - Check firewall rules: `netsh advfirewall firewall add rule name="RemoteC API" dir=in action=allow protocol=TCP localport=7001`
   - Verify API is listening: `netstat -an | findstr :7001`

2. **Host not appearing online**
   - Check Host logs for connection errors
   - Verify SignalR connection: Look for WebSocket upgrade in logs
   - Test network connectivity between machines

3. **Screen capture not working**
   - Ensure Host is running with appropriate permissions
   - On Windows, may need to run as Administrator
   - Check for GPU driver issues

4. **Authentication failures**
   - Verify Azure AD B2C configuration
   - Check JWT token expiration settings
   - Review API logs for authentication errors

### Debug Mode

To run in debug mode for troubleshooting:

```powershell
# API with verbose logging
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:Logging__LogLevel__Default="Debug"
dotnet run --project src\RemoteC.Api

# Host with console output
dotnet run --project src\RemoteC.Host --console
```

## Security Considerations

1. **Use HTTPS in Production**
   - Configure SSL certificates
   - Update all URLs to use https://

2. **Firewall Configuration**
   - Only open required ports
   - Restrict access to trusted networks

3. **Authentication**
   - Enable multi-factor authentication in Azure AD B2C
   - Use strong PIN policies

4. **Network Isolation**
   - Use VPN for remote access over internet
   - Implement network segmentation

## Next Steps

1. **Configure SSL/TLS**
   - See `docs/SSL_CONFIGURATION.md`

2. **Set up monitoring**
   - Configure Application Insights
   - Set up alerts for critical events

3. **Scale for production**
   - Deploy behind a load balancer
   - Configure Redis clustering
   - Set up SQL Server Always On

## Support

For issues or questions:
- Check logs in `/logs` directory
- Review `docs/TROUBLESHOOTING.md`
- Submit issues at https://github.com/dbbuilder/RemoteC/issues