# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY package.json ./
COPY package-lock.json* ./

# Install dependencies
RUN npm install

# Copy source code
COPY . .

# Debug: List files and show package.json scripts
RUN ls -la && echo "Package.json scripts:" && npm run

# Accept build arguments
ARG REACT_APP_API_URL
ARG REACT_APP_HUB_URL

# Build the application with VITE_ prefixed environment variables
ENV VITE_API_URL=$REACT_APP_API_URL
ENV VITE_HUB_URL=$REACT_APP_HUB_URL
ENV VITE_USE_DEV_AUTH=true
RUN npm run build

# Runtime stage
FROM nginx:alpine AS final

# Install wget for health checks (smaller than curl)
RUN apk add --no-cache wget

# Copy built application
COPY --from=build /app/dist /usr/share/nginx/html

# Create custom nginx configuration
RUN echo 'events { worker_connections 1024; } \
    http { \
        include /etc/nginx/mime.types; \
        default_type application/octet-stream; \
        server { \
            listen 80; \
            server_name localhost; \
            root /usr/share/nginx/html; \
            index index.html; \
            location / { \
                try_files $uri $uri/ /index.html; \
            } \
            location /health { \
                return 200 "OK"; \
                add_header Content-Type text/plain; \
            } \
        } \
    }' > /etc/nginx/nginx.conf

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --spider -q http://localhost/health || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]