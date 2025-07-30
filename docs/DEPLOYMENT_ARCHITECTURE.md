# RemoteC Deployment Architecture

## Overview

This document outlines the deployment architecture for RemoteC, covering development, staging, and production environments. The system is designed for cloud-native deployment with support for on-premises installations.

## Deployment Environments

### Development Environment
- **Purpose**: Active development and testing
- **Infrastructure**: Local Docker Compose
- **Database**: SQL Server Developer Edition in container
- **Redis**: Redis container
- **SSL**: Self-signed certificates

### Staging Environment
- **Purpose**: Pre-production testing and UAT
- **Infrastructure**: Azure Kubernetes Service (AKS)
- **Database**: Azure SQL Database (Basic tier)
- **Redis**: Azure Cache for Redis (Basic tier)
- **SSL**: Let's Encrypt certificates

### Production Environment
- **Purpose**: Live production system
- **Infrastructure**: Azure Kubernetes Service (AKS) with multiple regions
- **Database**: Azure SQL Database (Premium tier with geo-replication)
- **Redis**: Azure Cache for Redis (Premium tier with clustering)
- **SSL**: Azure-managed certificates

## Container Architecture

### Container Images

```yaml
# Base images used
RemoteC.Api: mcr.microsoft.com/dotnet/aspnet:8.0
RemoteC.Web: node:20-alpine (build), nginx:alpine (runtime)
RemoteC.Host: mcr.microsoft.com/dotnet/runtime:8.0-windowsservercore-ltsc2022
RemoteC.Client: mcr.microsoft.com/dotnet/runtime:8.0

# Image registry structure
remotec.azurecr.io/
├── api:latest
├── api:v1.0.0
├── web:latest
├── web:v1.0.0
├── host-windows:latest
├── host-linux:latest
├── client:latest
└── client:v1.0.0
```

### Docker Compose (Development)

```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: src/RemoteC.Api/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=RemoteCDb;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=true
      - Redis__ConnectionString=redis:6379
    ports:
      - "7001:80"
      - "7002:443"
    depends_on:
      - sqlserver
      - redis
    volumes:
      - ./src/RemoteC.Api/appsettings.Development.json:/app/appsettings.Development.json:ro

  web:
    build:
      context: ./src/RemoteC.Web
      dockerfile: Dockerfile
    environment:
      - REACT_APP_API_URL=https://localhost:7002
      - REACT_APP_SIGNALR_URL=https://localhost:7002/hubs/session
    ports:
      - "3000:80"
    depends_on:
      - api

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Password
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
      - ./database/setup-database.sql:/docker-entrypoint-initdb.d/setup.sql:ro

  redis:
    image: redis:7-alpine
    command: redis-server --appendonly yes
    ports:
      - "6379:6379"
    volumes:
      - redisdata:/data

volumes:
  sqldata:
  redisdata:
```

## Kubernetes Architecture

### Namespace Structure

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: remotec-prod
  labels:
    environment: production
    app: remotec
---
apiVersion: v1
kind: Namespace
metadata:
  name: remotec-staging
  labels:
    environment: staging
    app: remotec
```

### Core Deployments

#### API Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: remotec-api
  namespace: remotec-prod
spec:
  replicas: 3
  selector:
    matchLabels:
      app: remotec-api
  template:
    metadata:
      labels:
        app: remotec-api
    spec:
      containers:
      - name: api
        image: remotec.azurecr.io/api:v1.0.0
        ports:
        - containerPort: 80
        - containerPort: 443
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: remotec-secrets
              key: db-connection-string
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
```

#### Web Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: remotec-web
  namespace: remotec-prod
spec:
  replicas: 2
  selector:
    matchLabels:
      app: remotec-web
  template:
    metadata:
      labels:
        app: remotec-web
    spec:
      containers:
      - name: web
        image: remotec.azurecr.io/web:v1.0.0
        ports:
        - containerPort: 80
        resources:
          requests:
            memory: "128Mi"
            cpu: "100m"
          limits:
            memory: "256Mi"
            cpu: "200m"
```

### Service Definitions

```yaml
apiVersion: v1
kind: Service
metadata:
  name: remotec-api-service
  namespace: remotec-prod
spec:
  selector:
    app: remotec-api
  ports:
    - name: http
      port: 80
      targetPort: 80
    - name: https
      port: 443
      targetPort: 443
  type: ClusterIP
---
apiVersion: v1
kind: Service
metadata:
  name: remotec-web-service
  namespace: remotec-prod
spec:
  selector:
    app: remotec-web
  ports:
    - name: http
      port: 80
      targetPort: 80
  type: ClusterIP
```

### Ingress Configuration

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: remotec-ingress
  namespace: remotec-prod
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/proxy-body-size: "100m"
    nginx.ingress.kubernetes.io/proxy-read-timeout: "3600"
    nginx.ingress.kubernetes.io/proxy-send-timeout: "3600"
spec:
  tls:
  - hosts:
    - app.remotec.com
    - api.remotec.com
    secretName: remotec-tls
  rules:
  - host: app.remotec.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: remotec-web-service
            port:
              number: 80
  - host: api.remotec.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: remotec-api-service
            port:
              number: 80
```

### Horizontal Pod Autoscaling

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: remotec-api-hpa
  namespace: remotec-prod
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: remotec-api
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

## Azure Infrastructure

### Resource Groups

```
remotec-prod-rg/
├── remotec-aks-cluster (AKS)
├── remotec-acr (Container Registry)
├── remotec-sql-server (SQL Database)
├── remotec-redis (Redis Cache)
├── remotec-keyvault (Key Vault)
├── remotec-storage (Blob Storage)
├── remotec-appinsights (Application Insights)
└── remotec-loganalytics (Log Analytics)
```

### Azure SQL Database Configuration

```json
{
  "sku": {
    "name": "P1",
    "tier": "Premium",
    "capacity": 125
  },
  "properties": {
    "collation": "SQL_Latin1_General_CP1_CI_AS",
    "maxSizeBytes": 536870912000,
    "zoneRedundant": true,
    "readScale": "Enabled",
    "highAvailabilityReplicaCount": 2,
    "requestedBackupStorageRedundancy": "Geo"
  }
}
```

### Azure Redis Cache Configuration

```json
{
  "sku": {
    "name": "Premium",
    "family": "P",
    "capacity": 1
  },
  "properties": {
    "enableNonSslPort": false,
    "minimumTlsVersion": "1.2",
    "redisConfiguration": {
      "maxmemory-policy": "allkeys-lru",
      "rdb-backup-enabled": "true",
      "rdb-backup-frequency": "60",
      "rdb-backup-max-snapshot-count": "1"
    }
  }
}
```

## CI/CD Pipeline

### GitHub Actions Workflow

```yaml
name: Build and Deploy

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  REGISTRY: remotec.azurecr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Build and Test
      run: |
        dotnet build --configuration Release
        dotnet test --configuration Release --no-build
    
    - name: Build Docker Images
      run: |
        docker build -t $REGISTRY/api:${{ github.sha }} -f src/RemoteC.Api/Dockerfile .
        docker build -t $REGISTRY/web:${{ github.sha }} -f src/RemoteC.Web/Dockerfile .
    
    - name: Push to Registry
      if: github.ref == 'refs/heads/main'
      run: |
        echo ${{ secrets.REGISTRY_PASSWORD }} | docker login $REGISTRY -u ${{ secrets.REGISTRY_USERNAME }} --password-stdin
        docker push $REGISTRY/api:${{ github.sha }}
        docker push $REGISTRY/web:${{ github.sha }}

  deploy:
    needs: build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
    - name: Deploy to AKS
      uses: azure/k8s-deploy@v4
      with:
        manifests: |
          k8s/api-deployment.yaml
          k8s/web-deployment.yaml
        images: |
          $REGISTRY/api:${{ github.sha }}
          $REGISTRY/web:${{ github.sha }}
```

## Security Configuration

### Network Policies

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: api-network-policy
  namespace: remotec-prod
spec:
  podSelector:
    matchLabels:
      app: remotec-api
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: remotec-prod
    - podSelector:
        matchLabels:
          app: nginx-ingress
    ports:
    - protocol: TCP
      port: 80
    - protocol: TCP
      port: 443
  egress:
  - to:
    - namespaceSelector: {}
    ports:
    - protocol: TCP
      port: 1433  # SQL Server
    - protocol: TCP
      port: 6379  # Redis
    - protocol: TCP
      port: 443   # External APIs
```

### Secret Management

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: remotec-secrets
  namespace: remotec-prod
type: Opaque
data:
  db-connection-string: <base64-encoded-connection-string>
  redis-connection-string: <base64-encoded-redis-connection>
  azure-ad-client-secret: <base64-encoded-secret>
  encryption-key: <base64-encoded-key>
```

### Pod Security Policy

```yaml
apiVersion: policy/v1beta1
kind: PodSecurityPolicy
metadata:
  name: remotec-psp
spec:
  privileged: false
  allowPrivilegeEscalation: false
  requiredDropCapabilities:
    - ALL
  volumes:
    - 'configMap'
    - 'emptyDir'
    - 'projected'
    - 'secret'
    - 'downwardAPI'
    - 'persistentVolumeClaim'
  hostNetwork: false
  hostIPC: false
  hostPID: false
  runAsUser:
    rule: 'MustRunAsNonRoot'
  seLinux:
    rule: 'RunAsAny'
  fsGroup:
    rule: 'RunAsAny'
  readOnlyRootFilesystem: true
```

## Monitoring and Observability

### Prometheus Configuration

```yaml
apiVersion: v1
kind: ServiceMonitor
metadata:
  name: remotec-api-monitor
  namespace: remotec-prod
spec:
  selector:
    matchLabels:
      app: remotec-api
  endpoints:
  - port: metrics
    interval: 30s
    path: /metrics
```

### Grafana Dashboards

```json
{
  "dashboard": {
    "title": "RemoteC Production Dashboard",
    "panels": [
      {
        "title": "API Request Rate",
        "targets": [
          {
            "expr": "rate(http_requests_total{job=\"remotec-api\"}[5m])"
          }
        ]
      },
      {
        "title": "Active Sessions",
        "targets": [
          {
            "expr": "remotec_active_sessions_total"
          }
        ]
      },
      {
        "title": "Error Rate",
        "targets": [
          {
            "expr": "rate(http_requests_total{job=\"remotec-api\",status=~\"5..\"}[5m])"
          }
        ]
      }
    ]
  }
}
```

### Application Insights Integration

```csharp
// Kubernetes-specific telemetry
services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true;
});

services.AddSingleton<ITelemetryInitializer>(provider =>
    new KubernetesTelemetryInitializer
    {
        PodName = Environment.GetEnvironmentVariable("HOSTNAME"),
        NodeName = Environment.GetEnvironmentVariable("NODE_NAME"),
        Namespace = Environment.GetEnvironmentVariable("POD_NAMESPACE")
    });
```

## Disaster Recovery

### Backup Strategy

1. **Database Backups**
   - Automated backups every 4 hours
   - Point-in-time restore capability (35 days)
   - Geo-redundant backup storage

2. **Application State**
   - Redis snapshots every hour
   - Blob storage with geo-replication
   - Configuration stored in Git

3. **Container Images**
   - Images tagged with git commit SHA
   - Retention policy: 90 days for non-production
   - Geo-replicated container registry

### Recovery Procedures

```bash
# Database recovery
az sql db restore \
  --dest-name RemoteCDb-Restored \
  --edition Premium \
  --service-objective P2 \
  --source-database-id "/subscriptions/.../databases/RemoteCDb" \
  --restore-point-in-time "2024-01-15T10:30:00Z"

# Kubernetes rollback
kubectl rollout undo deployment/remotec-api -n remotec-prod
kubectl rollout status deployment/remotec-api -n remotec-prod

# Redis recovery
redis-cli --rdb /backup/dump.rdb
redis-cli FLUSHDB
redis-cli --pipe < /backup/redis-backup.txt
```

## Multi-Region Deployment

### Traffic Manager Configuration

```json
{
  "properties": {
    "profileStatus": "Enabled",
    "trafficRoutingMethod": "Performance",
    "dnsConfig": {
      "relativeName": "remotec-global",
      "ttl": 30
    },
    "monitorConfig": {
      "protocol": "HTTPS",
      "port": 443,
      "path": "/health",
      "intervalInSeconds": 30,
      "timeoutInSeconds": 10,
      "toleratedNumberOfFailures": 3
    },
    "endpoints": [
      {
        "name": "remotec-eastus",
        "type": "Microsoft.Network/trafficManagerProfiles/azureEndpoints",
        "properties": {
          "targetResourceId": "/subscriptions/.../remotec-eastus-ip",
          "endpointStatus": "Enabled",
          "priority": 1
        }
      },
      {
        "name": "remotec-westeurope",
        "type": "Microsoft.Network/trafficManagerProfiles/azureEndpoints",
        "properties": {
          "targetResourceId": "/subscriptions/.../remotec-westeurope-ip",
          "endpointStatus": "Enabled",
          "priority": 2
        }
      }
    ]
  }
}
```

## Cost Optimization

### Resource Allocation

```yaml
# Development/Staging - Burstable instances
dev:
  api:
    cpu: "100m-500m"
    memory: "256Mi-512Mi"
  web:
    cpu: "50m-200m"
    memory: "128Mi-256Mi"

# Production - Reserved instances with autoscaling
prod:
  api:
    cpu: "500m-2000m"
    memory: "512Mi-2Gi"
    replicas: "3-10"
  web:
    cpu: "100m-500m"
    memory: "256Mi-512Mi"
    replicas: "2-5"
```

### Cost Monitoring

```kusto
// Azure Cost Analysis Query
Resources
| where type =~ "microsoft.containerservice/managedclusters"
| extend clustername = name
| extend cost = todouble(tags['monthlyCost'])
| project clustername, location, cost
| order by cost desc
```

## Deployment Checklist

### Pre-Deployment
- [ ] All tests passing
- [ ] Security scan completed
- [ ] Database migrations tested
- [ ] Performance benchmarks met
- [ ] Documentation updated

### Deployment
- [ ] Create deployment branch
- [ ] Update version numbers
- [ ] Build and tag images
- [ ] Deploy to staging
- [ ] Run smoke tests
- [ ] Deploy to production (canary)
- [ ] Monitor metrics
- [ ] Complete rollout

### Post-Deployment
- [ ] Verify all services healthy
- [ ] Check error rates
- [ ] Monitor performance metrics
- [ ] Update status page
- [ ] Send deployment notification