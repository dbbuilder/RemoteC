#!/bin/bash

# RemoteC Build Script
# This script builds all components of the RemoteC solution

set -e

echo "Building RemoteC Solution..."

# Navigate to solution root
cd "$(dirname "$0")/.."

# Build .NET solution
echo "Building .NET solution..."
dotnet restore
dotnet build --configuration Release --no-restore

# Run tests
echo "Running unit tests..."
dotnet test --no-build --configuration Release --verbosity normal

# Build React frontend
echo "Building React frontend..."
cd src/RemoteC.Web
npm ci
npm run build
cd ../..

# Create deployment package
echo "Creating deployment package..."
mkdir -p deployment/build
dotnet publish src/RemoteC.Api -c Release -o deployment/build/api --no-build
cp -r src/RemoteC.Web/build deployment/build/web

echo "Build completed successfully!"
echo "Deployment files are in: deployment/build/"