.Data --startup-project src/RemoteC.Api

# Generate SQL script from migrations
dotnet ef migrations script --project src/RemoteC.Data --startup-project src/RemoteC.Api
```

### Frontend Development
```bash
# Start development server
cd src/RemoteC.Web
npm start

# Run tests
npm test

# Build for production
npm run build

# Lint and format code
npm run lint
npm run format
```

## Configuration

### Azure AD B2C Setup
1. Create Azure AD B2C tenant
2. Register application for RemoteC
3. Configure user flows (sign-up/sign-in)
4. Update `appsettings.Development.json` with tenant details

### ControlR Integration (Phase 1)
1. Obtain ControlR license/API access
2. Configure ControlR settings in `appsettings.json`
3. Test ControlR connectivity

### Redis Configuration
```bash
# Install Redis (Windows with Chocolatey)
choco install redis-64

# Install Redis (Ubuntu)
sudo apt-get install redis-server

# Start Redis service
redis-server

# Test Redis connection
redis-cli ping
```

## Troubleshooting

### Common Issues

#### Database Connection Issues
- Verify SQL Server is running
- Check connection string in `appsettings.Development.json`
- Ensure database exists and migrations are applied

#### React App Won't Start
- Clear node_modules: `rm -rf node_modules && npm install`
- Clear npm cache: `npm cache clean --force`
- Check for port conflicts

#### API Authentication Issues
- Verify Azure AD B2C configuration
- Check JWT token expiration
- Ensure correct redirect URLs

#### ControlR Integration Issues
- Verify ControlR service is running
- Check API keys and endpoints
- Review ControlR logs for errors

### Development Tips

#### Hot Reload
- API: Use `dotnet watch run` for automatic restart on code changes
- React: `npm start` includes hot reload by default

#### Debugging
- API: Use Visual Studio debugger or VS Code
- React: Browser developer tools and React Developer Tools extension

#### Performance Monitoring
- API: Application Insights integration included
- Database: Use SQL Server Profiler or Azure Data Studio
- React: React Developer Tools Profiler

## Project Structure Overview

```
RemoteC/
├── docs/                      # Documentation files
├── src/                       # Source code
│   ├── RemoteC.Api/          # ASP.NET Core Web API
│   ├── RemoteC.Web/          # React frontend
│   ├── RemoteC.Data/         # Entity Framework data layer
│   ├── RemoteC.Shared/       # Shared models and utilities
│   ├── RemoteC.Host/         # Host application (WPF)
│   ├── RemoteC.Client/       # Client application
│   └── RemoteC.Core/         # Rust performance layer (Phase 2)
├── tests/                     # Test projects
├── database/                  # Database scripts and migrations
├── deployment/                # Deployment configurations
├── scripts/                   # Build and utility scripts
└── docker-compose.yml        # Development environment
```

## Phase 1 Development Focus

### Core Features to Implement
1. **Authentication & Authorization**
   - Azure AD B2C integration
   - JWT token management
   - Role-based access control

2. **Session Management**
   - Create/start/stop sessions
   - PIN-based access
   - Real-time status updates

3. **ControlR Integration**
   - Remote control abstraction layer
   - Screen sharing functionality
   - Input event handling

4. **React Frontend**
   - Material-UI components
   - Real-time updates with SignalR
   - Responsive design

5. **Database Layer**
   - Entity Framework Core
   - Stored procedures only
   - Comprehensive audit logging

### Performance Targets (Phase 1)
- API response time: <200ms for 95th percentile
- Session establishment: <30 seconds
- Concurrent sessions: 100+ per server
- Database queries: <100ms for 99th percentile

## Next Steps

1. **Complete API Implementation**
   - Implement remaining service classes
   - Add comprehensive error handling
   - Setup logging and monitoring

2. **Frontend Development**
   - Create React components
   - Implement authentication flow
   - Add real-time session management

3. **ControlR Integration**
   - Setup ControlR development environment
   - Implement provider abstraction
   - Test remote control functionality

4. **Testing & Quality Assurance**
   - Unit tests for all components
   - Integration tests for workflows
   - Performance testing setup

5. **Documentation**
   - API documentation with Swagger
   - User guides and tutorials
   - Deployment procedures

For questions or issues, refer to the project documentation in the `docs/` folder or check the GitHub issues.