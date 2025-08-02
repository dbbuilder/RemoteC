#!/bin/bash

# SSL Certificate Setup for RemoteC Demo
# Generates self-signed certificates for HTTPS testing

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
SSL_DIR="$ROOT_DIR/deployment/ssl"

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

# Get machine IP or hostname
get_hostname() {
    local hostname="localhost"
    
    # Try to get machine IP
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        hostname=$(hostname -I | awk '{print $1}')
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        hostname=$(ifconfig | grep "inet " | grep -v 127.0.0.1 | awk '{print $2}' | head -1)
    fi
    
    # Allow custom hostname
    read -p "Enter hostname/IP for certificate (default: $hostname): " custom_host
    if [ -n "$custom_host" ]; then
        hostname="$custom_host"
    fi
    
    echo "$hostname"
}

# Create SSL directory
create_ssl_directory() {
    print_info "Creating SSL directory..."
    mkdir -p "$SSL_DIR"
    cd "$SSL_DIR"
    print_success "SSL directory created at: $SSL_DIR"
}

# Generate self-signed certificate
generate_certificate() {
    local hostname=$1
    
    print_info "Generating self-signed certificate for: $hostname"
    
    # Create certificate configuration
    cat > cert.conf <<EOF
[req]
default_bits = 2048
prompt = no
default_md = sha256
distinguished_name = dn
req_extensions = v3_req

[dn]
C = US
ST = State
L = City
O = RemoteC Demo
OU = IT Department
CN = $hostname

[v3_req]
subjectAltName = @alt_names

[alt_names]
DNS.1 = localhost
DNS.2 = $hostname
DNS.3 = *.remotec.local
IP.1 = 127.0.0.1
IP.2 = ::1
EOF

    # Add machine IP if it's an IP address
    if [[ $hostname =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        echo "IP.3 = $hostname" >> cert.conf
    fi
    
    # Generate private key
    openssl genrsa -out remotec.key 2048
    
    # Generate certificate signing request
    openssl req -new -key remotec.key -out remotec.csr -config cert.conf
    
    # Generate self-signed certificate (valid for 365 days)
    openssl x509 -req -days 365 -in remotec.csr -signkey remotec.key -out remotec.crt -extensions v3_req -extfile cert.conf
    
    # Create combined PEM file
    cat remotec.crt remotec.key > remotec.pem
    
    # Create PFX for Windows
    openssl pkcs12 -export -out remotec.pfx -inkey remotec.key -in remotec.crt -passout pass:
    
    # Set appropriate permissions
    chmod 600 remotec.key
    chmod 644 remotec.crt remotec.pem remotec.pfx
    
    # Clean up
    rm -f remotec.csr cert.conf
    
    print_success "SSL certificate generated successfully!"
}

# Create nginx configuration for SSL
create_nginx_config() {
    local hostname=$1
    
    print_info "Creating nginx configuration for SSL..."
    
    cat > "$ROOT_DIR/deployment/nginx/demo.conf" <<EOF
events {
    worker_connections 1024;
}

http {
    upstream api {
        server api:7001;
    }
    
    upstream web {
        server web:80;
    }
    
    # HTTP to HTTPS redirect
    server {
        listen 80;
        server_name $hostname;
        return 301 https://\$server_name\$request_uri;
    }
    
    # HTTPS server for API
    server {
        listen 443 ssl http2;
        server_name $hostname;
        
        ssl_certificate /etc/nginx/ssl/remotec.crt;
        ssl_certificate_key /etc/nginx/ssl/remotec.key;
        
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;
        ssl_prefer_server_ciphers on;
        
        # API and SignalR
        location ~ ^/(api|hubs|health|swagger) {
            proxy_pass http://api;
            proxy_http_version 1.1;
            proxy_set_header Upgrade \$http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host \$host;
            proxy_set_header X-Real-IP \$remote_addr;
            proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto \$scheme;
            proxy_read_timeout 86400;
        }
        
        # Web UI
        location / {
            proxy_pass http://web;
            proxy_set_header Host \$host;
            proxy_set_header X-Real-IP \$remote_addr;
            proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto \$scheme;
        }
    }
}
EOF
    
    mkdir -p "$ROOT_DIR/deployment/nginx"
    print_success "Nginx configuration created!"
}

# Update .env for SSL
update_env_for_ssl() {
    local hostname=$1
    
    print_info "Updating .env for SSL configuration..."
    
    cd "$ROOT_DIR"
    
    if [ -f .env ]; then
        # Backup current .env
        cp .env .env.backup
        
        # Update SSL settings
        if grep -q "SSL_ENABLED=" .env; then
            sed -i.bak "s/SSL_ENABLED=.*/SSL_ENABLED=true/" .env
        else
            echo "SSL_ENABLED=true" >> .env
        fi
        
        # Update URLs to HTTPS
        sed -i.bak "s|REACT_APP_API_URL=http://|REACT_APP_API_URL=https://|" .env
        sed -i.bak "s|REACT_APP_HUB_URL=http://|REACT_APP_HUB_URL=https://|" .env
        
        print_success ".env updated for SSL!"
    else
        print_warning ".env file not found. Run deploy-demo.sh first."
    fi
}

# Display instructions
display_instructions() {
    local hostname=$1
    
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}SSL Setup Complete!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo "Certificate Details:"
    echo -e "  Location:    ${BLUE}$SSL_DIR${NC}"
    echo -e "  Hostname:    ${BLUE}$hostname${NC}"
    echo -e "  Valid for:   ${BLUE}365 days${NC}"
    echo ""
    echo "Files Generated:"
    echo "  - remotec.crt  (Certificate)"
    echo "  - remotec.key  (Private Key)"
    echo "  - remotec.pem  (Combined)"
    echo "  - remotec.pfx  (Windows)"
    echo ""
    echo "To use HTTPS:"
    echo "1. Start with SSL profile:"
    echo -e "   ${YELLOW}docker-compose -f docker-compose.demo.yml --profile ssl up -d${NC}"
    echo ""
    echo "2. Access via HTTPS:"
    echo -e "   ${BLUE}https://$hostname${NC}"
    echo ""
    echo "3. Trust the certificate:"
    echo "   - Browser will show security warning"
    echo "   - Click 'Advanced' → 'Proceed to site'"
    echo "   - Or import remotec.crt to trusted certificates"
    echo ""
    echo -e "${YELLOW}Note: This is a self-signed certificate for testing only!${NC}"
    echo -e "${YELLOW}For production, use a proper certificate from a CA.${NC}"
}

# Main flow
main() {
    print_info "Starting SSL setup for RemoteC demo..."
    
    # Get hostname
    hostname=$(get_hostname)
    
    # Create directories
    create_ssl_directory
    
    # Generate certificate
    generate_certificate "$hostname"
    
    # Create nginx config
    create_nginx_config "$hostname"
    
    # Update .env
    update_env_for_ssl "$hostname"
    
    # Display instructions
    display_instructions "$hostname"
}

# Run main
main