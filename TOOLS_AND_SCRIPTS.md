# RemoteC Tools and Scripts Documentation

This document lists all the tools, scripts, and utilities created during the development and remediation process.

## Test Management Scripts

### 1. `run-all-tests.sh`
**Purpose**: Comprehensive test runner with reporting
```bash
./run-all-tests.sh
```
- Runs all test categories (unit, integration, performance)
- Generates detailed reports
- Creates remediation plans
- Outputs to `test-results/` directory

### 2. `quick-test-scan.sh`
**Purpose**: Fast test status check
```bash
./quick-test-scan.sh
```
- Quick overview of test failures
- Identifies patterns in failures
- No detailed output, just summary

### 3. `fix-all-test-issues.sh`
**Purpose**: Automated test fix script
```bash
./fix-all-test-issues.sh
```
- Creates backups before modifications
- Applies common test fixes
- Generates patches for manual review

### 4. `quick-test-summary.sh`
**Purpose**: Test execution with summary
```bash
./quick-test-summary.sh
```
- Runs tests and captures output
- Shows failed test count by assembly
- Lists sample failed tests

## Build and Development Scripts

### 5. `scripts/build.sh` / `scripts/build.bat`
**Purpose**: Cross-platform build scripts
```bash
# Linux/Mac
./scripts/build.sh

# Windows
scripts\build.bat
```
- Builds entire solution
- Runs basic smoke tests
- Packages outputs

### 6. `apply-test-fixes.sh`
**Purpose**: Apply automated test fixes
```bash
./apply-test-fixes.sh
```
- Fixes IConfiguration mock patterns
- Updates IDistributedCache mocks
- Adds TODO comments for manual fixes

### 7. `fix-moq-extension-methods.sh`
**Purpose**: Identify Moq extension method issues
```bash
./fix-moq-extension-methods.sh
```
- Finds files with extension method mocks
- Creates fix documentation
- Lists files needing attention

## Docker and Deployment

### 8. Docker Compose Commands
```bash
# Development environment
docker-compose up -d

# Production environment
docker-compose -f docker-compose.prod.yml up -d

# Clean up
docker-compose down -v
```

### 9. Kubernetes Deployment
```bash
# Deploy to Kubernetes
kubectl apply -f k8s/

# Check status
kubectl get pods -n remotec

# View logs
kubectl logs -f deployment/remotec-api -n remotec
```

## Database Management

### 10. Database Setup
```bash
# Run from WSL
sqlcmd -S 172.31.208.1,14333 -U sa -P YourPassword -i database/setup-database.sql

# Create stored procedures
sqlcmd -S 172.31.208.1,14333 -U sa -P YourPassword -i database/stored-procedures/create-all.sql
```

## Performance Testing

### 11. Run Performance Benchmarks
```bash
cd tests/RemoteC.Tests.Performance
dotnet run -c Release

# Specific benchmarks
dotnet run -c Release -- --filter "*Api*"
dotnet run -c Release -- --filter "*SignalR*"
```

## Code Quality Tools

### 12. Code Coverage
```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
```

### 13. Security Scanning
```bash
# Check for vulnerable packages
dotnet list package --vulnerable --include-transitive

# Run security audit
dotnet tool install -g security-scan
security-scan ./src
```

## Utility Functions

### 14. Test Helpers Created

#### ConfigurationHelper.cs
Location: `tests/RemoteC.Api.Tests/Helpers/ConfigurationHelper.cs`
- Creates in-memory configuration for tests
- Provides consistent test settings

#### TestBase.cs
Location: `tests/RemoteC.Api.Tests/TestBase.cs`
- Base class for all tests
- Handles common setup/teardown
- Provides shared services

## Git Helpers

### 15. Common Git Commands
```bash
# See what's changed
git status

# Commit with conventional commit message
git commit -m "fix: resolve test failures in PinServiceTests"

# Push to remote
git push origin main

# Create a new feature branch
git checkout -b feature/your-feature-name
```

## Monitoring and Logs

### 16. View Application Logs
```bash
# Docker logs
docker logs remotec-api -f

# Kubernetes logs
kubectl logs -f deployment/remotec-api

# Local development
tail -f logs/remotec-*.log
```

### 17. Health Check Commands
```bash
# Basic health
curl http://localhost:7001/health

# Detailed health
curl http://localhost:7001/health/ready

# Liveness check
curl http://localhost:7001/health/live
```

## Environment Setup

### 18. Environment Variables
```bash
# Development
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Server=localhost;Database=RemoteC;..."

# Production
export ASPNETCORE_ENVIRONMENT=Production
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-secret"
```

## Troubleshooting Commands

### 19. Common Troubleshooting
```bash
# Check port usage
netstat -tulpn | grep 7001

# Test SQL connection from WSL
sqlcmd -S 172.31.208.1,14333 -U sa -Q "SELECT 1"

# Clear Redis cache
redis-cli FLUSHALL

# Restart Docker containers
docker-compose restart
```

## CI/CD Commands

### 20. GitHub Actions
```bash
# Run workflow locally (requires act)
act -j build

# Check workflow syntax
actionlint .github/workflows/*.yml
```

## Usage Tips

1. **Always run scripts from the project root directory**
2. **Make scripts executable**: `chmod +x script-name.sh`
3. **Check script contents before running**: `cat script-name.sh`
4. **Use `-h` or `--help` flag when available**
5. **Scripts create backups before making changes**

## Script Maintenance

- Scripts are located in project root or `/scripts` directory
- All scripts have comments explaining their purpose
- Scripts use error handling and create logs
- Backups are created with timestamps
- Scripts are idempotent (safe to run multiple times)

## Future Improvements

1. **PowerShell versions** of all bash scripts
2. **Makefile** for common tasks
3. **Interactive setup wizard**
4. **Automated dependency checker**
5. **Performance baseline recorder**