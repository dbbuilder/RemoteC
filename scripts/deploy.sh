#!/bin/bash

# RemoteC Deployment Script
# This script handles deployment of the RemoteC application

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT=${1:-production}
DOCKER_REGISTRY=${DOCKER_REGISTRY:-ghcr.io/dbbuilder}
VERSION=${VERSION:-latest}

# Functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose is not installed"
        exit 1
    fi
    
    # Check environment file
    if [ ! -f ".env.$ENVIRONMENT" ]; then
        log_error "Environment file .env.$ENVIRONMENT not found"
        exit 1
    fi
    
    log_info "Prerequisites check passed"
}

build_images() {
    log_info "Building Docker images..."
    
    # Build API image
    docker build -f Dockerfile.api -t $DOCKER_REGISTRY/remotec-api:$VERSION .
    
    # Build Web image
    docker build -f Dockerfile.web -t $DOCKER_REGISTRY/remotec-web:$VERSION .
    
    log_info "Docker images built successfully"
}

push_images() {
    log_info "Pushing images to registry..."
    
    # Push API image
    docker push $DOCKER_REGISTRY/remotec-api:$VERSION
    
    # Push Web image
    docker push $DOCKER_REGISTRY/remotec-web:$VERSION
    
    log_info "Images pushed successfully"
}

deploy_stack() {
    log_info "Deploying application stack..."
    
    # Load environment variables
    export $(cat .env.$ENVIRONMENT | xargs)
    
    # Deploy using docker-compose
    if [ "$ENVIRONMENT" == "production" ]; then
        docker-compose -f docker-compose.prod.yml up -d
    else
        docker-compose up -d
    fi
    
    log_info "Application deployed successfully"
}

run_migrations() {
    log_info "Running database migrations..."
    
    # Wait for database to be ready
    sleep 10
    
    # Run migrations
    docker-compose run --rm db-migrator
    
    log_info "Database migrations completed"
}

health_check() {
    log_info "Performing health checks..."
    
    # Wait for services to start
    sleep 20
    
    # Check API health
    if curl -f http://localhost:8080/health > /dev/null 2>&1; then
        log_info "API health check passed"
    else
        log_error "API health check failed"
        exit 1
    fi
    
    # Check Web health
    if curl -f http://localhost/health > /dev/null 2>&1; then
        log_info "Web health check passed"
    else
        log_error "Web health check failed"
        exit 1
    fi
}

# Main execution
main() {
    log_info "Starting RemoteC deployment for environment: $ENVIRONMENT"
    
    check_prerequisites
    
    if [ "$2" == "--build" ]; then
        build_images
        push_images
    fi
    
    deploy_stack
    
    if [ "$2" == "--migrate" ]; then
        run_migrations
    fi
    
    health_check
    
    log_info "Deployment completed successfully!"
    log_info "Application is running at:"
    log_info "  - Web UI: http://localhost"
    log_info "  - API: http://localhost:8080"
}

# Run main function
main "$@"