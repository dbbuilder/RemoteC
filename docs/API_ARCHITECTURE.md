# RemoteC API Architecture

## Overview

The RemoteC API is built on ASP.NET Core 8.0 following RESTful principles and real-time communication patterns. This document details the API architecture, design patterns, and implementation guidelines.

## API Structure

### Controller Organization

```
src/RemoteC.Api/Controllers/
├── AuthController.cs       # Authentication & authorization
├── DevicesController.cs    # Device management
├── SessionsController.cs   # Remote session management
└── FileTransferController.cs # File transfer operations
```

### Service Layer Architecture

```
src/RemoteC.Api/Services/
├── Core Services
│   ├── SessionService.cs         # Session lifecycle management
│   ├── UserService.cs            # User management
│   ├── PinService.cs             # PIN authentication
│   └── FileTransferService.cs    # File transfer logic
├── Security Services
│   ├── AuditService.cs           # Audit logging
│   ├── EncryptionService.cs      # Data encryption
│   ├── E2EEncryptionService.cs   # End-to-end encryption
│   └── CertificateService.cs     # Certificate management
├── Enterprise Services
│   ├── ComplianceService.cs      # Compliance management
│   ├── PolicyEngineService.cs    # Policy enforcement
│   ├── AnalyticsService.cs       # Usage analytics
│   └── IdentityProviderService.cs # External IdP integration
└── Infrastructure Services
    ├── CacheService.cs           # Redis caching
    ├── MetricsCollector.cs       # Performance metrics
    ├── BackgroundTaskQueue.cs    # Background job processing
    └── HealthCheckService.cs     # Health monitoring
```

## Design Patterns

### 1. Repository Pattern

All database access goes through repositories that exclusively use stored procedures:

```csharp
public interface ISessionRepository
{
    Task<Session?> GetSessionAsync(Guid sessionId);
    Task<Session> CreateSessionAsync(CreateSessionRequest request);
    Task UpdateSessionStatusAsync(Guid sessionId, SessionStatus status);
}

public class SessionRepository : ISessionRepository
{
    private readonly RemoteCDbContext _context;
    
    public async Task<Session?> GetSessionAsync(Guid sessionId)
    {
        return await _context.Sessions
            .FromSqlRaw("EXEC sp_GetSession @SessionId", 
                new SqlParameter("@SessionId", sessionId))
            .FirstOrDefaultAsync();
    }
}
```

### 2. Service Layer Pattern

Business logic is encapsulated in service classes:

```csharp
public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<SessionService> _logger;
    
    public async Task<SessionDto> CreateSessionAsync(
        CreateSessionRequest request, 
        string userId)
    {
        // Validate request
        // Create session
        // Audit the action
        // Return DTO
    }
}
```

### 3. Provider Pattern

Remote control providers can be swapped:

```csharp
public interface IRemoteControlProvider
{
    Task<string> StartSessionAsync(Guid sessionId, string deviceId);
    Task StopSessionAsync(Guid sessionId);
    Task<byte[]> GetScreenshotAsync(Guid sessionId);
}

// Current implementation
public class ControlRProvider : IRemoteControlProvider { }

// Future implementation
public class RustProvider : IRemoteControlProvider { }
```

## API Endpoints

### Authentication Endpoints

```http
POST   /api/auth/login              # Azure AD B2C login
GET    /api/auth/profile            # Get user profile
PUT    /api/auth/profile            # Update profile
GET    /api/auth/permissions        # Get user permissions
POST   /api/auth/validate-pin       # Validate session PIN
```

### Device Management Endpoints

```http
GET    /api/devices                 # List user devices (paginated)
GET    /api/devices/{id}            # Get device details
POST   /api/devices/register        # Register new device
PATCH  /api/devices/{id}/status     # Update device status
DELETE /api/devices/{id}            # Delete device
```

### Session Management Endpoints

```http
GET    /api/sessions                # List user sessions
GET    /api/sessions/{id}           # Get session details
POST   /api/sessions                # Create new session
POST   /api/sessions/{id}/start     # Start remote control
POST   /api/sessions/{id}/stop      # Stop remote control
POST   /api/sessions/{id}/pin       # Generate PIN
```

### File Transfer Endpoints

```http
POST   /api/filetransfer/initiate              # Start new transfer
POST   /api/filetransfer/upload-chunk          # Upload file chunk
GET    /api/filetransfer/{id}/status           # Get transfer status
GET    /api/filetransfer/{id}/download/{chunk} # Download chunk
POST   /api/filetransfer/{id}/cancel           # Cancel transfer
POST   /api/filetransfer/cleanup-stalled       # Admin: cleanup stalled
```

## Real-time Communication (SignalR)

### Hub Structure

```csharp
[Authorize]
public class SessionHub : Hub
{
    // Connection lifecycle
    public override async Task OnConnectedAsync() { }
    public override async Task OnDisconnectedAsync(Exception? exception) { }
    
    // Session management
    public async Task JoinSession(string sessionId) { }
    public async Task LeaveSession(string sessionId) { }
    
    // Input streaming
    public async Task SendMouseInput(string sessionId, MouseInputDto input) { }
    public async Task SendKeyboardInput(string sessionId, KeyboardInputDto input) { }
    
    // Screen updates
    public async Task SendScreenUpdate(string sessionId, ScreenUpdateDto screenData) { }
    
    // Control management
    public async Task RequestControl(string sessionId) { }
    public async Task GrantControl(string sessionId, string targetUserId) { }
}
```

### Client Methods (Called from Server)

```javascript
// JavaScript/TypeScript client
connection.on("UserJoinedSession", (userId) => { });
connection.on("UserLeftSession", (userId) => { });
connection.on("ReceiveMouseInput", (input) => { });
connection.on("ReceiveKeyboardInput", (input) => { });
connection.on("ReceiveScreenUpdate", (screenData) => { });
connection.on("ControlGranted", (userId) => { });
```

## Security Implementation

### 1. Authentication Middleware

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = configuration["AzureAdB2C:Authority"];
        options.Audience = configuration["AzureAdB2C:ClientId"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });
```

### 2. Authorization Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", 
        policy => policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireDeviceAccess", 
        policy => policy.Requirements.Add(new DeviceAccessRequirement()));
    
    options.AddPolicy("RequireSessionOwner", 
        policy => policy.Requirements.Add(new SessionOwnerRequirement()));
});
```

### 3. Audit Middleware

```csharp
public class AuditMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Capture request details
        var auditLog = new AuditLog
        {
            UserId = context.User.GetUserId(),
            Action = $"{context.Request.Method} {context.Request.Path}",
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            Timestamp = DateTime.UtcNow
        };
        
        // Execute request
        await _next(context);
        
        // Log result
        auditLog.Success = context.Response.StatusCode < 400;
        await _auditService.LogAsync(auditLog);
    }
}
```

## Data Transfer Objects (DTOs)

### Request DTOs

```csharp
public class CreateSessionRequest
{
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public SessionType SessionType { get; set; }
}

public class FileTransferRequest
{
    public Guid SessionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public TransferDirection Direction { get; set; }
}
```

### Response DTOs

```csharp
public class SessionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DeviceDto Device { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ParticipantDto> Participants { get; set; }
}

public class FileTransferDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public long TotalSize { get; set; }
    public int ChunkSize { get; set; }
    public int TotalChunks { get; set; }
    public TransferStatus Status { get; set; }
}
```

## Error Handling

### Global Exception Handler

```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            await HandleBusinessException(context, ex);
        }
        catch (ValidationException ex)
        {
            await HandleValidationException(context, ex);
        }
        catch (Exception ex)
        {
            await HandleGenericException(context, ex);
        }
    }
}
```

### Standard Error Response

```json
{
    "error": {
        "code": "SESSION_NOT_FOUND",
        "message": "The requested session does not exist",
        "details": {
            "sessionId": "123e4567-e89b-12d3-a456-426614174000"
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "traceId": "00-1234567890abcdef-1234567890abcdef-00"
    }
}
```

## Performance Optimization

### 1. Caching Strategy

```csharp
public class SessionService
{
    private readonly ICacheService _cache;
    
    public async Task<SessionDto?> GetSessionAsync(Guid sessionId, string userId)
    {
        var cacheKey = $"session:{sessionId}";
        
        // Try cache first
        var cached = await _cache.GetAsync<SessionDto>(cacheKey);
        if (cached != null) return cached;
        
        // Load from database
        var session = await _sessionRepository.GetSessionAsync(sessionId);
        if (session != null)
        {
            await _cache.SetAsync(cacheKey, session, TimeSpan.FromMinutes(5));
        }
        
        return session;
    }
}
```

### 2. Database Query Optimization

- All queries use stored procedures
- Proper indexing on foreign keys and frequently queried columns
- Connection pooling configured
- Async/await throughout

### 3. Response Compression

```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
```

## API Versioning

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new HeaderApiVersionReader("X-API-Version");
});
```

## Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<RemoteCDbContext>("database")
    .AddRedis("redis")
    .AddAzureBlobStorage("blob-storage")
    .AddAzureKeyVault("key-vault");

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

## OpenAPI/Swagger Configuration

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RemoteC API",
        Version = "v1",
        Description = "Enterprise Remote Control Solution API"
    });
    
    // Add JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    
    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
```

## Testing Strategy

### Unit Tests
- Service layer logic
- Repository methods
- Utility functions

### Integration Tests
- API endpoint testing with TestServer
- Database operations with TestContainers
- SignalR hub testing

### Performance Tests
- Load testing with NBomber
- Stress testing critical endpoints
- Memory leak detection

## Monitoring and Observability

### Application Insights Integration

```csharp
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
```

### Custom Metrics

```csharp
public class MetricsCollector : IMetricsCollector
{
    private readonly TelemetryClient _telemetryClient;
    
    public void RecordSessionDuration(TimeSpan duration)
    {
        _telemetryClient.TrackMetric("Session.Duration", duration.TotalSeconds);
    }
    
    public void RecordFileTransferSpeed(double bytesPerSecond)
    {
        _telemetryClient.TrackMetric("FileTransfer.Speed", bytesPerSecond);
    }
}
```

## Deployment Considerations

### Environment Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=#{SQL_SERVER};Database=#{DB_NAME};User Id=#{DB_USER};Password=#{DB_PASSWORD};"
  },
  "Redis": {
    "ConnectionString": "#{REDIS_CONNECTION_STRING}"
  },
  "AzureAdB2C": {
    "Instance": "https://#{TENANT_NAME}.b2clogin.com",
    "ClientId": "#{CLIENT_ID}",
    "Domain": "#{TENANT_NAME}.onmicrosoft.com"
  }
}
```

### Docker Configuration

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/RemoteC.Api/RemoteC.Api.csproj", "src/RemoteC.Api/"]
RUN dotnet restore "src/RemoteC.Api/RemoteC.Api.csproj"
COPY . .
WORKDIR "/src/src/RemoteC.Api"
RUN dotnet build "RemoteC.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RemoteC.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RemoteC.Api.dll"]
```

## Future Enhancements

1. **GraphQL Support**: Add GraphQL endpoint for flexible querying
2. **gRPC Services**: High-performance service-to-service communication
3. **Event Sourcing**: Implement event sourcing for audit trail
4. **CQRS Pattern**: Separate read and write models for scalability
5. **API Gateway**: Implement Ocelot or YARP for API gateway functionality