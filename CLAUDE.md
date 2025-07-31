# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RemoteC is an enterprise-grade remote control solution with a three-phase evolution:
- **Phase 1**: ControlR integration MVP with React UI and Azure AD B2C
- **Phase 2**: Custom Rust performance engine for RustDesk-level performance
- **Phase 3**: Enterprise features with advanced RBAC and compliance

## Build and Development Commands

### Backend (.NET)
```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run the API locally
cd src/RemoteC.Api
dotnet run

# Apply database migrations
dotnet ef database update -p src/RemoteC.Data -s src/RemoteC.Api
```

### Frontend (React)
```bash
cd src/RemoteC.Web

# Install dependencies
npm install

# Run development server
npm start

# Run tests
npm test

# Build for production
npm run build

# Lint and format
npm run lint
npm run lint:fix
npm run format
```

### Full Stack Development
```bash
# Using Docker Compose (recommended for development)
docker-compose up -d

# Using build scripts
./scripts/build.sh        # Linux/Mac
scripts\build.bat         # Windows
```

### Database
```bash
# Connect to SQL Server from WSL
sqlcmd -S 172.31.208.1,14333 -U sv -P YourPassword -C -d RemoteC2Db

# Run database setup
sqlcmd -S localhost -i database/setup-database.sql
```

## Architecture Overview

### Solution Structure
- **RemoteC.Api**: ASP.NET Core 8.0 Web API with SignalR hubs
- **RemoteC.Web**: React 18 + TypeScript frontend with Material-UI
- **RemoteC.Data**: Entity Framework Core data layer (SQL Server with stored procedures only)
- **RemoteC.Shared**: Shared models and DTOs
- **RemoteC.Core**: (Phase 2) Rust performance components with FFI
- **RemoteC.Host**: Windows host application for remote control
- **RemoteC.Client**: Cross-platform client application

### Key Design Patterns
1. **Repository Pattern**: All database access through repositories calling stored procedures
2. **Provider Pattern**: Abstraction for remote control providers (ControlR → Custom Rust)
3. **Service Layer**: Business logic separated from controllers
4. **Real-time Communication**: SignalR for live session updates and control

### Database Access
- **IMPORTANT**: All database operations MUST use stored procedures
- No direct SQL or LINQ queries in application code
- Entity Framework is configured for stored procedure mapping only
- Connection strings use Azure Key Vault in production

### Authentication Flow
1. User authenticates via Azure AD B2C
2. JWT tokens issued with role claims
3. SignalR hubs authenticated with bearer tokens
4. PIN-based quick access for simplified connections

### Phase-Specific Considerations

#### Phase 1 (Current)
- ControlR integration through IRemoteControlProvider interface
- Focus on UI/UX and enterprise authentication
- Performance target: <100ms latency on LAN

#### Phase 2 (Future)
- Rust components will replace ControlR
- FFI interface for .NET/Rust communication
- Performance target: <50ms latency (RustDesk parity)

## Development Guidelines

### Testing Requirements
- Unit test coverage target: 80%
- All new features require tests
- Integration tests for API endpoints
- E2E tests for critical user flows

### Code Style
- C#: Follow Microsoft conventions with EditorConfig
- TypeScript: Airbnb style guide (enforced by ESLint)
- SQL: Consistent stored procedure naming (sp_EntityAction)

### Security Considerations
- All endpoints require authentication (except health checks)
- Use parameterized stored procedures to prevent SQL injection
- Sensitive data encrypted with Azure Key Vault
- Audit logging for all critical operations

### Performance Optimization
- Use Redis for session state and caching
- Implement pagination for all list endpoints
- SignalR for real-time updates (avoid polling)
- Lazy loading for React components

## Common Development Tasks

### Adding a New API Endpoint
1. Define the model in RemoteC.Shared
2. Create stored procedure in database/stored-procedures/
3. Add repository method in RemoteC.Data
4. Implement service method in RemoteC.Api/Services
5. Create controller action with proper authorization
6. Add unit and integration tests

### Adding a New React Component
1. Create component in appropriate directory under src/RemoteC.Web/src
2. Use Material-UI components for consistency
3. Implement with TypeScript and proper type definitions
4. Add to routing if it's a page component
5. Include unit tests with React Testing Library

### Modifying Database Schema
1. Update Entity Framework models in RemoteC.Data/Entities
2. Create migration script in database/migrations
3. Update relevant stored procedures
4. Test migration on development database
5. Update seed data if necessary

## Troubleshooting

### WSL to SQL Server Connection Issues
- Use WSL host IP (172.31.208.1) instead of localhost
- Always include -C flag for certificate trust
- Check SQL Server port configuration (may be 14333 instead of 1433)

### SignalR Connection Problems
- Ensure CORS is properly configured
- Check authentication token is being sent
- Verify WebSocket support in deployment environment

### Docker Compose Issues
- Ensure Docker Desktop is running
- Check port conflicts (1433, 6379, 7001, 3000)
- Verify network connectivity between containers

## Code Snippets and Utilities

### JWT Token Date Handling
- Helper method to convert DateTime to Unix timestamp, handling default/unset dates
```csharp
// Helper: Check for default DateTime, which means "not set"
long? ToUnixIfSet(DateTime dt) =>
    (dt != DateTime.MinValue && dt != DateTime.MaxValue)
        ? new DateTimeOffset(dt).ToUnixTimeSeconds()
        : (long?)null;

// Example usage in JWT token payload
Exp = jwtToken != null ? ToUnixIfSet(jwtToken.Payload.ValidTo) : null,
Iat = jwtToken != null ? ToUnixIfSet(jwtToken.Payload.ValidFrom) : null,
```