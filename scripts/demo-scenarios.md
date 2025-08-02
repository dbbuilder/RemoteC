# RemoteC Demo Test Scenarios

## Quick Test Scenarios for Demo Deployment

### 1. Basic Health Check (1 minute)
```bash
# Check all services are running
docker-compose -f docker-compose.demo.yml ps

# Test API health
curl http://localhost:7001/health

# Test Web UI
curl http://localhost:3000
```

### 2. Authentication Test (2 minutes)
```bash
# Login as admin
curl -X POST http://localhost:7001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@remotec.demo","password":"Admin@123"}'

# Login as user
curl -X POST http://localhost:7001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@remotec.demo","password":"User@123"}'
```

### 3. Cross-Network Test (5 minutes)

**Machine A (Server):**
1. Run deployment: `./scripts/deploy-demo.sh`
2. Note the IP address shown (e.g., 192.168.1.100)

**Machine B (Client):**
1. Open browser to: `http://192.168.1.100:3000`
2. Login with demo credentials
3. Test features:
   - Create a new session
   - Join with PIN
   - Test screen sharing

### 4. Performance Test (3 minutes)
```bash
# Run network test
./scripts/test-demo-network.sh -s http://192.168.1.100:7001

# Monitor resource usage
docker stats

# Check logs for errors
docker-compose -f docker-compose.demo.yml logs -f --tail=100
```

### 5. Load Test (5 minutes)
```bash
# Install Apache Bench if needed
sudo apt-get install apache2-utils  # Ubuntu/Debian
brew install ab                      # macOS

# Run load test
ab -n 1000 -c 10 http://localhost:7001/health

# Test SignalR connections
for i in {1..10}; do
  curl -s http://localhost:7001/hubs/remoteControl &
done
```

### 6. Security Test (2 minutes)
```bash
# Test unauthorized access
curl -I http://localhost:7001/api/sessions

# Test CORS
curl -H "Origin: http://evil.com" \
     -H "Access-Control-Request-Method: GET" \
     -H "Access-Control-Request-Headers: X-Requested-With" \
     -X OPTIONS http://localhost:7001/api/health
```

### 7. Mobile Device Test (3 minutes)
1. Get your machine's IP: `hostname -I` (Linux) or `ipconfig` (Windows)
2. On mobile device connected to same network:
   - Open browser to `http://YOUR_IP:3000`
   - Test responsive UI
   - Try joining a session

### 8. Recovery Test (2 minutes)
```bash
# Simulate API crash
docker-compose -f docker-compose.demo.yml kill api

# Watch auto-restart
docker-compose -f docker-compose.demo.yml ps

# Verify recovery
curl http://localhost:7001/health
```

### 9. Database Connection Test
```bash
# Connect to database
docker-compose -f docker-compose.demo.yml exec db /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -d RemoteC2Db \
  -Q "SELECT COUNT(*) FROM sys.tables"

# Check demo data
docker-compose -f docker-compose.demo.yml exec db /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -d RemoteC2Db \
  -Q "SELECT Email FROM Users"
```

### 10. Full Demo Flow (10 minutes)

1. **Setup** (2 min)
   - Deploy on server machine
   - Open on client machine
   
2. **Admin Tasks** (3 min)
   - Login as admin
   - Create new user
   - View audit logs
   - Check system status
   
3. **User Session** (3 min)
   - Login as user
   - Start new session
   - Share PIN with another user
   - Test remote control
   
4. **Performance** (2 min)
   - Multiple users join same session
   - Monitor latency
   - Test bandwidth usage

## Demo Tips

1. **Before Demo:**
   - Pre-deploy and test
   - Clear browser cache
   - Have backup machine ready
   - Test network connectivity

2. **During Demo:**
   - Start with local test
   - Progress to network test
   - Show monitoring tools
   - Demonstrate security features

3. **Common Issues:**
   - Port blocked: Check firewall
   - Can't connect: Verify IP address
   - Slow performance: Check Docker resources
   - Login fails: Check demo accounts created

## Quick Commands Reference

```bash
# Start demo
./scripts/deploy-demo.sh

# View logs
./scripts/deploy-demo.sh logs

# Check status
./scripts/deploy-demo.sh status

# Stop demo
./scripts/deploy-demo.sh stop

# Clean everything
./scripts/deploy-demo.sh clean

# Test network
./scripts/test-demo-network.sh -s http://YOUR_IP:7001
```