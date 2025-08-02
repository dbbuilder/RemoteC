# RemoteC Demo - Quick Start Guide

## 🚀 30-Second Setup

### Windows
```powershell
git clone https://github.com/your-org/remotec.git
cd remotec
.\scripts\deploy-demo.ps1
```

### Linux/Mac
```bash
git clone https://github.com/your-org/remotec.git
cd remotec
./scripts/deploy-demo.sh
```

**That's it!** Access the demo at:
- Local: http://localhost:3000
- Network: http://YOUR-IP:3000

## 📱 Test From Any Device

1. **Find your IP**: The deployment script shows it
2. **Open browser** on any device on same network
3. **Login with**:
   - Admin: `admin@remotec.demo` / `Admin@123`
   - User: `user@remotec.demo` / `User@123`

## 🧪 Quick Tests

### Test 1: Local Connection
```bash
curl http://localhost:7001/health
# Should return: {"status":"Healthy"}
```

### Test 2: Network Access
From another machine:
```bash
./scripts/test-demo-network.sh -s http://SERVER-IP:7001
```

### Test 3: Remote Control
1. Start host on one machine
2. Connect from another using PIN
3. Test screen sharing and control

## 🛑 Management

```bash
# View logs
./scripts/deploy-demo.sh logs

# Check status
./scripts/deploy-demo.sh status

# Stop demo
./scripts/deploy-demo.sh stop

# Clean up
./scripts/deploy-demo.sh clean
```

## 🔒 Enable HTTPS (Optional)

```bash
# Setup SSL certificates
./scripts/setup-ssl.sh

# Start with HTTPS
docker-compose -f docker-compose.demo.yml --profile ssl up -d
```

## ❓ Troubleshooting

**Can't access from network?**
- Check firewall (ports 3000, 7001)
- Verify IP address matches
- Ensure on same network

**Services won't start?**
- Check Docker is running
- Free up ports: `docker-compose -f docker-compose.demo.yml down`
- Clean and retry: `./scripts/deploy-demo.sh clean && ./scripts/deploy-demo.sh`

**Need help?**
- Full guide: [DEMO_DEPLOYMENT_GUIDE.md](DEMO_DEPLOYMENT_GUIDE.md)
- Test scenarios: [scripts/demo-scenarios.md](scripts/demo-scenarios.md)

---
🎯 **Pro Tip**: Run deployment before your demo and test network access first!