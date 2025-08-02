# RemoteC Deployment Methods Comparison

## Method 1: Docker-based Build (Original)
- **Build Time**: 60-90 minutes (clean build)
- **Process**: Builds inside Docker containers
- **Pros**: Single command, no local dependencies needed
- **Cons**: Very slow, downloads all dependencies each time

## Method 2: Local Build + Docker (New)
- **Build Time**: 2-5 minutes (clean build)
- **Process**: Builds locally, then creates minimal Docker images
- **Pros**: 10-20x faster, better caching, easier debugging
- **Cons**: Requires .NET SDK and Node.js installed locally

## Quick Start Guide

### Fast Deployment (Recommended)
```powershell
# First time setup - installs dependencies and builds
./scripts/fast-deploy.ps1

# Subsequent deploys (if code hasn't changed)
./scripts/fast-deploy.ps1 -NoBuild

# Clean rebuild
./scripts/fast-deploy.ps1 -Clean
```

### Traditional Docker Build (Slow)
```powershell
# Original method - very slow but works without local tools
./scripts/deploy-demo-clean.ps1
```

## Build Time Breakdown

### Docker Build (deploy-demo-clean.ps1)
- API Build: ~75 minutes
  - Base image download: 5 min
  - System packages: 10 min
  - .NET restore: 20 min
  - Compilation: 40 min
- Web Build: ~15 minutes
  - Base image: 2 min
  - npm install: 10 min
  - Build: 3 min

### Local Build (fast-deploy.ps1)
- API Build: ~90 seconds
  - .NET restore: 30 sec (cached after first run)
  - Compilation: 60 sec
- Web Build: ~60 seconds
  - npm install: 0 sec (if node_modules exists)
  - Build: 60 sec
- Docker image creation: ~10 seconds total

## Prerequisites

### For Fast Deployment
- .NET 8.0 SDK: https://dotnet.microsoft.com/download
- Node.js 18+: https://nodejs.org/
- Docker Desktop

### For Docker-only Deployment
- Docker Desktop only

## Recommendations

1. **For Development**: Use `fast-deploy.ps1` for quick iteration
2. **For Testing Clean Builds**: Use `deploy-demo-clean.ps1` occasionally
3. **For CI/CD**: Can use either, but local build is much faster