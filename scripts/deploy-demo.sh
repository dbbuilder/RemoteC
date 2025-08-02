#!/bin/bash

# RemoteC Demo Deployment Script
# One-click deployment for testing across networks

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# Project name for docker-compose
PROJECT_NAME="remotec-demo"

echo -e "${BLUE}RemoteC Demo Deployment System${NC}"
echo "================================"

# Function to print colored output
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    print_info "Checking prerequisites..."
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install Docker first."
        exit 1
    fi
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is not installed. Please install Docker Compose first."
        exit 1
    fi
    
    # Check if Docker is running
    if ! docker info &> /dev/null; then
        print_error "Docker is not running. Please start Docker."
        exit 1
    fi
    
    print_success "All prerequisites met!"
}

# Get machine IP address
get_machine_ip() {
    local ip=""
    
    # Try different methods to get IP
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Linux
        ip=$(hostname -I | awk '{print $1}')
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        ip=$(ifconfig | grep "inet " | grep -v 127.0.0.1 | awk '{print $2}' | head -1)
    elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then
        # Windows
        ip=$(ipconfig | grep -A 4 'Ethernet adapter' | grep 'IPv4' | awk '{print $NF}')
    fi
    
    # Fallback to localhost if can't determine
    if [ -z "$ip" ]; then
        ip="localhost"
    fi
    
    echo "$ip"
}

# Setup environment
setup_environment() {
    print_info "Setting up environment..."
    
    cd "$ROOT_DIR"
    
    # Check if .env exists
    if [ ! -f .env ]; then
        print_info "Creating .env file from template..."
        cp .env.template .env
        
        # Get machine IP
        MACHINE_IP=$(get_machine_ip)
        print_info "Detected machine IP: $MACHINE_IP"
        
        # Update .env with machine IP
        if [[ "$OSTYPE" == "darwin"* ]]; then
            # macOS
            sed -i '' "s/HOST_IP=.*/HOST_IP=$MACHINE_IP/" .env
            sed -i '' "s|REACT_APP_API_URL=.*|REACT_APP_API_URL=http://$MACHINE_IP:7001|" .env
            sed -i '' "s|REACT_APP_HUB_URL=.*|REACT_APP_HUB_URL=http://$MACHINE_IP:7001/hubs|" .env
        else
            # Linux/Windows
            sed -i "s/HOST_IP=.*/HOST_IP=$MACHINE_IP/" .env
            sed -i "s|REACT_APP_API_URL=.*|REACT_APP_API_URL=http://$MACHINE_IP:7001|" .env
            sed -i "s|REACT_APP_HUB_URL=.*|REACT_APP_HUB_URL=http://$MACHINE_IP:7001/hubs|" .env
        fi
        
        # Generate JWT secret if not set
        JWT_SECRET=$(openssl rand -hex 32 2>/dev/null || echo "demo-secret-key-for-testing-only-32-characters-long")
        if [[ "$OSTYPE" == "darwin"* ]]; then
            sed -i '' "s/JWT_SECRET=.*/JWT_SECRET=$JWT_SECRET/" .env
        else
            sed -i "s/JWT_SECRET=.*/JWT_SECRET=$JWT_SECRET/" .env
        fi
        
        print_success ".env file created and configured!"
    else
        print_warning ".env file already exists. Using existing configuration."
        MACHINE_IP=$(grep HOST_IP .env | cut -d'=' -f2)
        print_info "Using IP from .env: $MACHINE_IP"
    fi
}

# Build images
build_images() {
    print_info "Building Docker images..."
    
    docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml build --parallel
    
    print_success "Docker images built successfully!"
}

# Start services
start_services() {
    print_info "Starting services..."
    
    # Stop any running services first
    docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml down
    
    # Start services
    docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml up -d
    
    print_info "Waiting for services to be ready..."
    
    # Wait for database
    print_info "Waiting for SQL Server..."
    for i in {1..30}; do
        if docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml exec -T db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "SELECT 1" &> /dev/null; then
            print_success "SQL Server is ready!"
            break
        fi
        echo -n "."
        sleep 2
    done
    
    # Initialize database
    print_info "Initializing database..."
    docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml exec -T db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "
        IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RemoteC2Db')
        BEGIN
            CREATE DATABASE RemoteC2Db;
        END
    "
    
    # Wait for API
    print_info "Waiting for API..."
    for i in {1..30}; do
        if curl -s http://localhost:7001/health &> /dev/null; then
            print_success "API is ready!"
            break
        fi
        echo -n "."
        sleep 2
    done
    
    # Wait for Web
    print_info "Waiting for Web UI..."
    for i in {1..30}; do
        if curl -s http://localhost:3000 &> /dev/null; then
            print_success "Web UI is ready!"
            break
        fi
        echo -n "."
        sleep 2
    done
    
    print_success "All services started successfully!"
}

# Create demo data
create_demo_data() {
    print_info "Creating demo data..."
    
    # Run migrations
    print_info "Running database migrations..."
    docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml exec -T api dotnet ef database update
    
    # Seed demo data via API
    print_info "Seeding demo accounts..."
    
    # Wait a bit for API to be fully ready
    sleep 5
    
    # Create admin user
    curl -X POST http://localhost:7001/api/auth/register \
        -H "Content-Type: application/json" \
        -d '{
            "email": "admin@remotec.demo",
            "password": "Admin@123",
            "fullName": "Demo Admin",
            "role": "Admin"
        }' &> /dev/null || true
    
    # Create regular user
    curl -X POST http://localhost:7001/api/auth/register \
        -H "Content-Type: application/json" \
        -d '{
            "email": "user@remotec.demo",
            "password": "User@123",
            "fullName": "Demo User",
            "role": "User"
        }' &> /dev/null || true
    
    print_success "Demo data created!"
}

# Display access information
display_info() {
    local machine_ip=$1
    
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}RemoteC Demo Deployment Complete!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo "Access URLs:"
    echo -e "  Web UI (Local):    ${BLUE}http://localhost:3000${NC}"
    echo -e "  Web UI (Network):  ${BLUE}http://$machine_ip:3000${NC}"
    echo -e "  API (Local):       ${BLUE}http://localhost:7001${NC}"
    echo -e "  API (Network):     ${BLUE}http://$machine_ip:7001${NC}"
    echo ""
    echo "Demo Accounts:"
    echo -e "  Admin: ${YELLOW}admin@remotec.demo${NC} / ${YELLOW}Admin@123${NC}"
    echo -e "  User:  ${YELLOW}user@remotec.demo${NC} / ${YELLOW}User@123${NC}"
    echo ""
    echo "Services Status:"
    docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml ps
    echo ""
    echo "Quick Commands:"
    echo "  View logs:         docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml logs -f"
    echo "  Stop services:     docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml down"
    echo "  Restart services:  docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml restart"
    echo "  Clean everything:  docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml down -v"
    echo ""
    echo -e "${YELLOW}Note: For network access, ensure firewall allows ports 3000 and 7001${NC}"
}

# Main deployment flow
main() {
    print_info "Starting RemoteC demo deployment..."
    
    # Change to root directory
    cd "$ROOT_DIR"
    
    # Run deployment steps
    check_prerequisites
    setup_environment
    
    # Load environment variables
    source .env
    
    # Build and start
    build_images
    start_services
    create_demo_data
    
    # Display access information
    display_info "$HOST_IP"
}

# Handle script arguments
case "$1" in
    "stop")
        print_info "Stopping RemoteC demo..."
        cd "$ROOT_DIR"
        docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml down
        print_success "Services stopped!"
        ;;
    "clean")
        print_warning "Cleaning RemoteC demo (this will delete all data)..."
        cd "$ROOT_DIR"
        docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml down -v
        rm -f .env
        print_success "Clean complete!"
        ;;
    "status")
        cd "$ROOT_DIR"
        docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml ps
        ;;
    "logs")
        cd "$ROOT_DIR"
        docker-compose -p $PROJECT_NAME -f docker-compose.demo.yml logs -f
        ;;
    *)
        main
        ;;
esac