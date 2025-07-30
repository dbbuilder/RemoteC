# RemoteC Deployment Guide

This guide explains how to deploy and test RemoteC between two machines.

## Quick Start (Development Mode)

### Prerequisites
- .NET 8.0 SDK installed on both machines
- PowerShell or Command Prompt
- Firewall configured to allow ports 17001-17003

### Server Machine Setup

1. **Clone the Repository**
   ```bash
   git clone https://github.com/dbbuilder/RemoteC.git
   cd RemoteC
   ```

2. **Start the Development Server**
   ```bash
   # Windows Command Prompt
   scripts\start-dev-server.bat
   
   # PowerShell
   .\scripts\start-dev-server.ps1
   ```

   The server will start on:
   - HTTP: `http://localhost:17001`
   - HTTPS: `https://localhost:17003`

3. **Verify Server is Running**
   ```bash
   # Test health endpoint
   curl http://localhost:17001/health
   ```

### Client Machine Setup

1. **Install RemoteC Client**
   ```bash
   # Clone repository
   git clone https://github.com/dbbuilder/RemoteC.git
   cd RemoteC
   
   # Navigate to client
   cd src\RemoteC.Client
   
   # Build and run
   dotnet run -- --server http://SERVER_IP:17001
   ```

2. **Connect to Server**
   - Replace `SERVER_IP` with the actual IP address of the server machine
   - Use the PIN displayed on the server for quick connection

## Production Deployment

### Using Docker

1. **Server Deployment**
   ```bash
   # Build server image
   docker build -f docker/Dockerfile.api -t remotec-server .
   
   # Run server container
   docker run -d \
     -p 17001:80 \
     -p 17003:443 \
     -e ASPNETCORE_ENVIRONMENT=Production \
     -e ConnectionStrings__DefaultConnection="Your SQL Server Connection" \
     -e AzureAdB2C__TenantId="Your Tenant ID" \
     --name remotec-server \
     remotec-server
   ```

2. **Host Deployment (Windows)**
   ```bash
   # Build host image
   docker build -f docker/Dockerfile.host -t remotec-host .
   
   # Run host container
   docker run -d \
     -e SERVER_URL=http://SERVER_IP:17001 \
     --name remotec-host \
     remotec-host
   ```

3. **Client Deployment**
   ```bash
   # Build client image
   docker build -f docker/Dockerfile.client -t remotec-client .
   
   # Run client container
   docker run -it \
     -e SERVER_URL=http://SERVER_IP:17001 \
     remotec-client
   ```

### Manual Production Deployment

1. **Prepare Server**
   ```bash
   # Build for production
   cd src/RemoteC.Api
   dotnet publish -c Release -o /opt/remotec/server
   
   # Create service file (Linux)
   sudo nano /etc/systemd/system/remotec-server.service
   ```

   Service file content:
   ```ini
   [Unit]
   Description=RemoteC Server
   After=network.target
   
   [Service]
   Type=simple
   User=remotec
   WorkingDirectory=/opt/remotec/server
   ExecStart=/usr/bin/dotnet /opt/remotec/server/RemoteC.Api.dll
   Restart=always
   RestartSec=10
   Environment="ASPNETCORE_ENVIRONMENT=Production"
   Environment="ASPNETCORE_URLS=http://0.0.0.0:17001;https://0.0.0.0:17003"
   
   [Install]
   WantedBy=multi-user.target
   ```

2. **Configure Nginx (Optional)**
   ```nginx
   server {
       listen 80;
       server_name remotec.yourdomain.com;
       
       location / {
           proxy_pass http://localhost:17001;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
       
       location /hubs/session {
           proxy_pass http://localhost:17001;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection "upgrade";
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
       }
   }
   ```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | Development |
| `ASPNETCORE_URLS` | Server URLs | http://localhost:17001 |
| `ConnectionStrings__DefaultConnection` | Database connection | SQLite in dev |
| `ConnectionStrings__Redis` | Redis connection | Empty in dev |
| `Hangfire__Enabled` | Enable Hangfire | false in dev |
| `KeyVault__VaultName` | Azure Key Vault name | Empty in dev |

### Security Configuration

1. **Azure AD B2C** (Production)
   ```json
   {
     "AzureAdB2C": {
       "Instance": "https://yourtenant.b2clogin.com",
       "Domain": "yourtenant.onmicrosoft.com",
       "TenantId": "your-tenant-id",
       "ClientId": "your-client-id",
       "SignUpSignInPolicyId": "B2C_1_SignUpSignIn"
     }
   }
   ```

2. **JWT Configuration**
   ```json
   {
     "Security": {
       "JwtSecret": "your-production-secret",
       "JwtExpirationMinutes": 120,
       "SessionEncryptionKey": "your-encryption-key"
     }
   }
   ```

## Testing Between Two Machines

### Basic Connectivity Test

1. **On Server Machine**
   ```bash
   # Start server
   scripts\start-dev-server.bat
   
   # Note the server IP address
   ipconfig
   ```

2. **On Client Machine**
   ```bash
   # Test connectivity
   ping SERVER_IP
   
   # Test HTTP endpoint
   curl http://SERVER_IP:17001/health
   
   # Connect with client
   cd src\RemoteC.Client
   dotnet run -- --server http://SERVER_IP:17001
   ```

### Performance Testing

1. **Network Latency**
   ```bash
   # Test network latency
   ping -t SERVER_IP
   ```

2. **Bandwidth Test**
   ```bash
   # Use iperf3 for bandwidth testing
   # Server machine
   iperf3 -s -p 5201
   
   # Client machine
   iperf3 -c SERVER_IP -p 5201
   ```

### Troubleshooting

1. **Connection Refused**
   - Check firewall settings
   - Verify server is listening on all interfaces (0.0.0.0)
   - Check if ports are already in use

2. **Authentication Issues**
   - In development, authentication may be disabled
   - Check Azure AD B2C configuration in production
   - Verify JWT token configuration

3. **Performance Issues**
   - Check network bandwidth and latency
   - Monitor CPU and memory usage
   - Review SignalR connection settings

## Monitoring

### Health Checks
- `/health` - Overall health status
- `/health/ready` - Database and cache readiness
- `/health/live` - Liveness probe

### Metrics
- Application Insights (production)
- Serilog file logs
- Performance counters

## Scaling

### Horizontal Scaling
1. Deploy multiple API instances behind a load balancer
2. Use Redis for distributed caching and SignalR backplane
3. Configure sticky sessions for SignalR connections

### Vertical Scaling
- Increase CPU/memory for high-resolution screen sharing
- Use SSD storage for session recordings
- Optimize network bandwidth for multiple concurrent sessions