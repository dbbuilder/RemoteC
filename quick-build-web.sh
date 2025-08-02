#!/bin/bash
echo "🚀 Quick build for web app..."

cd src/RemoteC.Web

# Build without Docker first
echo "📦 Building locally..."
npm run build

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    
    # Create a simple Dockerfile just for serving
    cat > Dockerfile.quick <<EOF
FROM nginx:alpine
COPY dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EOF

    # Create nginx config
    cat > nginx.conf <<EOF
events { worker_connections 1024; }
http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;
    server {
        listen 80;
        server_name localhost;
        root /usr/share/nginx/html;
        index index.html;
        location / {
            try_files \$uri \$uri/ /index.html;
        }
        location /health {
            return 200 "OK";
            add_header Content-Type text/plain;
        }
    }
}
EOF

    # Build Docker image with pre-built files
    echo "🐳 Creating Docker image..."
    docker build -f Dockerfile.quick -t remotec-web-quick .
    
    # Run the container
    echo "🚀 Starting container..."
    docker stop remotec-web 2>/dev/null
    docker rm remotec-web 2>/dev/null
    docker run -d --name remotec-web --network remotec-demo_remotec-network -p 3000:80 remotec-web-quick
    
    echo "✅ Done! Check http://localhost:3000"
else
    echo "❌ Build failed!"
    exit 1
fi