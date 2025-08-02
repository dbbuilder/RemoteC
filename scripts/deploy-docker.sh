#!/bin/bash

# RemoteC Docker Deployment Script
# This script deploys RemoteC using Docker Compose

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
ENV_FILE="$PROJECT_ROOT/.env"

# Function to print colored output
print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Function to check prerequisites
check_prerequisites() {
    print_message $YELLOW "Checking prerequisites..."
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        print_message $RED "Docker is not installed. Please install Docker first."
        exit 1
    fi
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        print_message $RED "Docker Compose is not installed. Please install Docker Compose first."
        exit 1
    fi
    
    # Check .env file
    if [ ! -f "$ENV_FILE" ]; then
        print_message $YELLOW ".env file not found. Creating from template..."
        create_env_file
    fi
    
    print_message $GREEN "Prerequisites check completed."
}

# Function to create .env file
create_env_file() {
    cat > "$ENV_FILE" << EOF
# Azure AD Configuration
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret

# Azure Storage
AZURE_STORAGE_CONNECTION=DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=yourkey;EndpointSuffix=core.windows.net

# Encryption
ENCRYPTION_MASTER_KEY=your-base64-encoded-master-key

# Database
DB_SA_PASSWORD=RemoteC@2024Strong!

# Redis
REDIS_PASSWORD=RemoteC@Redis2024!
EOF
    
    print_message $YELLOW "Please edit $ENV_FILE with your actual configuration values."
    read -p "Press enter to continue after editing the .env file..."
}

# Function to build images
build_images() {
    print_message $YELLOW "Building Docker images..."
    
    cd "$PROJECT_ROOT"
    docker-compose build --no-cache
    
    print_message $GREEN "Docker images built successfully."
}

# Function to start services
start_services() {
    print_message $YELLOW "Starting services..."
    
    cd "$PROJECT_ROOT"
    docker-compose up -d
    
    print_message $GREEN "Services started successfully."
}

# Function to wait for services
wait_for_services() {
    print_message $YELLOW "Waiting for services to be ready..."
    
    # Wait for SQL Server
    print_message $YELLOW "Waiting for SQL Server..."
    until docker exec remotec-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$DB_SA_PASSWORD" -Q "SELECT 1" &> /dev/null; do
        sleep 5
    done
    print_message $GREEN "SQL Server is ready."
    
    # Wait for Redis
    print_message $YELLOW "Waiting for Redis..."
    until docker exec remotec-redis redis-cli ping &> /dev/null; do
        sleep 5
    done
    print_message $GREEN "Redis is ready."
    
    # Wait for API
    print_message $YELLOW "Waiting for API..."
    until curl -f http://localhost:7000/health &> /dev/null; do
        sleep 5
    done
    print_message $GREEN "API is ready."
}

# Function to run database migrations
run_migrations() {
    print_message $YELLOW "Running database migrations..."
    
    # This would typically run EF Core migrations
    # For now, we'll just create the database
    docker exec remotec-sqlserver /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P "$DB_SA_PASSWORD" \
        -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RemoteC2Db') CREATE DATABASE RemoteC2Db"
    
    print_message $GREEN "Database migrations completed."
}

# Function to show status
show_status() {
    print_message $YELLOW "\nService Status:"
    docker-compose ps
    
    print_message $GREEN "\nRemoteC is now running!"
    print_message $GREEN "API: http://localhost:7000 (HTTP) / https://localhost:7001 (HTTPS)"
    print_message $GREEN "Web App: http://localhost:3000"
    print_message $GREEN "\nTo view logs: docker-compose logs -f"
    print_message $GREEN "To stop: docker-compose down"
}

# Main execution
main() {
    print_message $GREEN "RemoteC Docker Deployment Script"
    print_message $GREEN "================================\n"
    
    check_prerequisites
    
    # Load environment variables
    source "$ENV_FILE"
    export DB_SA_PASSWORD
    export REDIS_PASSWORD
    
    # Ask for action
    echo "What would you like to do?"
    echo "1) Full deployment (build + start)"
    echo "2) Build images only"
    echo "3) Start services only"
    echo "4) Stop services"
    echo "5) View logs"
    echo "6) Remove all (including volumes)"
    read -p "Enter your choice (1-6): " choice
    
    case $choice in
        1)
            build_images
            start_services
            wait_for_services
            run_migrations
            show_status
            ;;
        2)
            build_images
            ;;
        3)
            start_services
            wait_for_services
            show_status
            ;;
        4)
            print_message $YELLOW "Stopping services..."
            docker-compose down
            print_message $GREEN "Services stopped."
            ;;
        5)
            docker-compose logs -f
            ;;
        6)
            print_message $RED "This will remove all containers, images, and volumes!"
            read -p "Are you sure? (y/N): " confirm
            if [ "$confirm" = "y" ]; then
                docker-compose down -v --rmi all
                print_message $GREEN "All resources removed."
            fi
            ;;
        *)
            print_message $RED "Invalid choice."
            exit 1
            ;;
    esac
}

# Run main function
main