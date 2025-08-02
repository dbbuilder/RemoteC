#!/bin/bash

# RemoteC Build Script
# This script builds all components of the RemoteC application

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
log_info() {
    echo -e "${GREEN}[BUILD]${NC} $1"
}

log_section() {
    echo -e "\n${YELLOW}=== $1 ===${NC}\n"
}

# Build .NET solution
build_dotnet() {
    log_section "Building .NET Solution"
    
    log_info "Restoring NuGet packages..."
    dotnet restore
    
    log_info "Building solution..."
    dotnet build -c Release
    
    log_info "Running tests..."
    dotnet test --no-build -c Release
    
    log_info ".NET build completed successfully"
}

# Build React application
build_react() {
    log_section "Building React Application"
    
    cd src/RemoteC.Web
    
    log_info "Installing npm packages..."
    npm ci
    
    log_info "Running linter..."
    npm run lint
    
    log_info "Running tests..."
    npm test -- --run
    
    log_info "Building production bundle..."
    npm run build
    
    cd ../..
    
    log_info "React build completed successfully"
}

# Build Docker images
build_docker() {
    log_section "Building Docker Images"
    
    log_info "Building API image..."
    docker build -f Dockerfile.api -t remotec-api:latest .
    
    log_info "Building Web image..."
    docker build -f Dockerfile.web -t remotec-web:latest .
    
    log_info "Docker build completed successfully"
}

# Main execution
main() {
    log_section "RemoteC Build Process"
    
    # Parse arguments
    BUILD_TARGET=${1:-all}
    
    case $BUILD_TARGET in
        dotnet)
            build_dotnet
            ;;
        react)
            build_react
            ;;
        docker)
            build_docker
            ;;
        all)
            build_dotnet
            build_react
            build_docker
            ;;
        *)
            echo "Usage: $0 [dotnet|react|docker|all]"
            exit 1
            ;;
    esac
    
    log_section "Build Completed Successfully!"
}

# Run main function
main "$@"