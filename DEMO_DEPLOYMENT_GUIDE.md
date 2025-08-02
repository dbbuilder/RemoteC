# RemoteC Demo Deployment Guide

This guide provides step-by-step instructions for deploying RemoteC demo system for testing across different networks.

## 🚀 Quick Start

### One-Click Deployment

#### Windows
```powershell
# Open PowerShell as Administrator (for firewall configuration)
cd path\to\remotec
.\scripts\deploy-demo.ps1
```

#### Linux/macOS
```bash
cd path/to/remotec
./scripts/deploy-demo.sh
```

That's it! The script will automatically:
- ✅ Check prerequisites
- ✅ Configure environment
- ✅ Build Docker images
- ✅ Start all services
- ✅ Create demo accounts
- ✅ Configure firewall (Windows)
- ✅ Display access URLs

## 📋 Prerequisites

1. **Docker Desktop** (Windows/macOS) or Docker Engine (Linux)
   - Download: https://www.docker.com/products/docker-desktop/
   - Ensure Docker is running

2. **Docker Compose** (usually included with Docker Desktop)

3. **Available Ports**:
   - 3000 (Web UI)
   - 7001 (API)
   - 1433 (SQL Server - internal)
   - 6379 (Redis - internal)

## 🌐 Network Testing Setup

### Local Network Testing

1. **Deploy on Server Machine**:
   ```bash
   # The script automatically detects your machine's IP
   ./scripts/deploy-demo.sh
   ```

2. **Access from Other Machines**:
   - Note the network URLs displayed after deployment
   - Example: `http://192.168.1.100:3000`
   - Ensure both machines are on the same network

3. **Firewall Configuration**:
   - **Windows**: Script configures automatically (if run as admin)
   - **Linux**: 
     ```bash
     sudo ufw allow 3000/tcp
     sudo ufw allow 7001/tcp
     ```
   - **macOS**: Usually not needed for local network

### Internet/WAN Testing

For testing across the internet:

1. **Port Forwarding** (Router Configuration):
   - Forward external port 3000 → internal_ip:3000
   - Forward external port 7001 → internal_ip:7001

2. **Dynamic DNS** (Optional):
   - Use services like DuckDNS, No-IP for stable hostname
   - Update `.env` with your domain

3. **SSL/HTTPS** (Recommended for Internet):
   ```bash
   # Generate self-signed certificate
   openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes

   # Update .env
   SSL_ENABLED=true
   SSL_CERT_PATH=/path/to/cert.pem
   SSL_KEY_PATH=/path/to/key.pem
   ```

## 🎯 Demo Scenarios

### Scenario 1: Basic Remote Control

1. **Login as Admin**:
   - URL: `http://localhost:3000`
   - Email: `admin@remotec.demo`
   - Password: `Admin@123`

2. **Start Host on Target Machine**:
   ```powershell
   # Windows
   .\scripts\start-host.ps1 -ServerUrl http://server-ip:7001
   ```

3. **Connect Using PIN**:
   - Note the 6-digit PIN from host
   - Enter PIN in web UI
   - Start remote control session

### Scenario 2: Multi-User Collaboration

1. **Create Additional Users**:
   - Login as admin
   - Navigate to User Management
   - Create test users

2. **Test Concurrent Sessions**:
   - Multiple users connect to same host
   - Test screen sharing vs control modes

### Scenario 3: Performance Testing

1. **LAN Performance Test**:
   ```bash
   # Run network latency test
   ./scripts/test-network-connection.ps1 -ServerUrl http://server-ip:7001
   ```

2. **Load Testing**:
   - Connect multiple clients simultaneously
   - Monitor resource usage with: `docker stats`

## 🛠️ Management Commands

### Service Management

```bash
# View status
./scripts/deploy-demo.sh status

# View logs
./scripts/deploy-demo.sh logs

# Stop services
./scripts/deploy-demo.sh stop

# Clean everything (removes all data)
./scripts/deploy-demo.sh clean
```

### Troubleshooting

```bash
# Check service health
docker-compose ps

# View specific service logs
docker-compose logs api
docker-compose logs web
docker-compose logs db

# Restart a service
docker-compose restart api

# Access database
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd"
```

## 📊 Monitoring

### Built-in Health Checks

- API Health: `http://localhost:7001/health`
- API Metrics: `http://localhost:7001/metrics`
- SignalR Status: `http://localhost:7001/hubs/remoteControl`

### Resource Monitoring

```bash
# Real-time resource usage
docker stats

# Check disk usage
docker system df
```

## 🔒 Security Considerations

### Demo Environment

The demo uses simplified security for easy testing:
- Default passwords (change for production)
- Self-signed certificates (if SSL enabled)
- Open network access (configure firewall as needed)

### Production Deployment

For production, ensure:
1. Strong passwords in `.env`
2. Valid SSL certificates
3. Proper firewall rules
4. Azure AD B2C configuration
5. Network segmentation

## 🐛 Common Issues

### Issue: Cannot access from other machines

**Solution**:
1. Check firewall settings
2. Verify IP address in browser matches server IP
3. Ensure `.env` has correct `HOST_IP`

### Issue: Services fail to start

**Solution**:
1. Check port conflicts: `netstat -an | grep -E '3000|7001|1433|6379'`
2. Ensure Docker has enough resources
3. Clean and restart: `./scripts/deploy-demo.sh clean && ./scripts/deploy-demo.sh`

### Issue: Database connection errors

**Solution**:
1. Wait for SQL Server to fully start (can take 30-60 seconds)
2. Check SQL Server logs: `docker-compose logs db`
3. Verify connection string in `.env`

## 📚 Additional Resources

- [Architecture Overview](docs/ARCHITECTURE.md)
- [API Documentation](http://localhost:7001/swagger)
- [Deployment Guide](docs/DEPLOYMENT_GUIDE.md)
- [Development Setup](docs/DEVELOPMENT.md)

## 💡 Tips for Effective Demo

1. **Prepare Demo Environment**:
   - Pre-create test accounts
   - Have host machines ready
   - Test network connectivity first

2. **Demo Flow**:
   - Start with local testing
   - Progress to LAN testing
   - Show performance metrics
   - Demonstrate security features

3. **Have Backup Plan**:
   - Keep screenshots of working system
   - Have recorded demo video
   - Prepare offline demo if needed

## 🆘 Getting Help

- Check logs: `docker-compose logs -f`
- GitHub Issues: [Report issues](https://github.com/your-org/remotec/issues)
- Documentation: See `/docs` directory

---

**Note**: This demo deployment is configured for testing and demonstration purposes. For production deployment, please refer to the full [Deployment Guide](docs/DEPLOYMENT_GUIDE.md).