# RemoteC Performance Optimization Guide

## Overview

This guide provides recommendations for optimizing RemoteC's performance based on our performance testing framework and industry best practices.

## Current Performance Targets (Phase 1)

- **Screen Capture Latency**: <100ms
- **Network Latency**: <100ms on LAN
- **API Response Time**: <300ms
- **SignalR Connection Time**: <500ms

## Future Performance Targets (Phase 2 - Rust Engine)

- **Screen Capture Latency**: <50ms
- **Network Latency**: <50ms (using QUIC protocol)
- **Frame Rate**: 60 FPS sustained capture
- **Encoding**: Hardware-accelerated H.264/H.265

## Optimization Strategies

### 1. Database Optimization

#### Indexing Strategy
```sql
-- Add covering indexes for frequent queries
CREATE INDEX IX_Sessions_Status_CreatedAt 
ON Sessions(Status, CreatedAt DESC) 
INCLUDE (Name, DeviceId);

CREATE INDEX IX_Devices_IsOnline_LastSeenAt 
ON Devices(IsOnline, LastSeenAt DESC) 
INCLUDE (Name, MacAddress);

-- Add filtered indexes for active records
CREATE INDEX IX_Users_Active 
ON Users(Email) 
WHERE IsActive = 1;
```

#### Query Optimization
- Use stored procedures for all database operations (already implemented)
- Enable query store for performance monitoring
- Consider read replicas for reporting queries
- Implement database connection pooling

#### Caching Strategy
- Redis caching for frequently accessed data
- Cache session information for 5 minutes
- Cache device status for 30 seconds
- Implement cache-aside pattern

### 2. API Performance

#### Response Caching
```csharp
// Add response caching for static data
[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
[HttpGet("devices")]
public async Task<IActionResult> GetDevices()
{
    // Implementation
}
```

#### Output Caching
```csharp
// In Program.cs
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => 
        builder.Expire(TimeSpan.FromMinutes(5)));
    
    options.AddPolicy("DeviceList", builder =>
        builder.Expire(TimeSpan.FromMinutes(10))
               .Tag("devices"));
});
```

#### Compression
```csharp
// Enable response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
```

### 3. SignalR Optimization

#### MessagePack Protocol
```csharp
// Use MessagePack for smaller payload sizes
builder.Services.AddSignalR()
    .AddMessagePackProtocol(options =>
    {
        options.SerializerOptions = MessagePackSerializerOptions.Standard
            .WithSecurity(MessagePackSecurity.UntrustedData);
    });
```

#### Backpressure Handling
```csharp
public class SessionHub : Hub
{
    private readonly IBackpressureService _backpressure;
    
    public async Task SendScreenUpdate(string sessionId, ScreenUpdateDto update)
    {
        // Check if client can handle the load
        if (await _backpressure.ShouldThrottle(Context.ConnectionId))
        {
            // Skip frame or reduce quality
            update = ReduceQuality(update);
        }
        
        await Clients.Group(sessionId).SendAsync("ReceiveScreenUpdate", update);
    }
}
```

#### Connection Management
- Implement connection pooling
- Use Azure SignalR Service for scale-out scenarios
- Configure appropriate keep-alive intervals
- Implement reconnection logic with exponential backoff

### 4. Screen Capture Optimization

#### Current Implementation (ControlR)
- Uses Windows Desktop Duplication API
- Captures only changed regions
- Basic JPEG compression

#### Phase 2 Optimization (Rust Engine)
```rust
// High-performance screen capture
pub struct ScreenCapture {
    // Use OS-specific APIs for best performance
    #[cfg(windows)]
    duplicator: DesktopDuplicator,
    
    #[cfg(target_os = "macos")]
    display_stream: CGDisplayStream,
    
    #[cfg(target_os = "linux")]
    x11_capture: X11Capture,
}

impl ScreenCapture {
    pub async fn capture_frame(&mut self) -> Result<Frame, Error> {
        // Capture with minimal latency
        let raw_frame = self.capture_raw()?;
        
        // Detect changed regions
        let changes = self.detect_changes(&raw_frame)?;
        
        // Return only changed data
        Ok(Frame {
            timestamp: Instant::now(),
            regions: changes,
            full_frame: if changes.is_full_update { 
                Some(raw_frame) 
            } else { 
                None 
            },
        })
    }
}
```

#### Video Encoding
- Hardware-accelerated H.264/H.265 encoding
- Adaptive bitrate based on network conditions
- Frame interpolation for smooth playback
- Implement B-frames for better compression

### 5. Network Optimization

#### QUIC Protocol (Phase 2)
```rust
// Use QUIC for lower latency
pub struct QuicTransport {
    endpoint: quinn::Endpoint,
    connections: HashMap<SessionId, quinn::Connection>,
}

impl QuicTransport {
    pub async fn send_frame(&self, session: SessionId, frame: &[u8]) -> Result<()> {
        let conn = self.connections.get(&session)?;
        
        // Send over unreliable stream for lowest latency
        let mut stream = conn.open_uni().await?;
        stream.write_all(frame).await?;
        stream.finish().await?;
        
        Ok(())
    }
}
```

#### CDN Integration
- Use Azure CDN for static assets
- Implement edge caching for API responses
- Use geo-distributed endpoints

### 6. Infrastructure Optimization

#### Container Optimization
```dockerfile
# Multi-stage build for smaller images
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out --no-self-contained -p:PublishTrimmed=true

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "RemoteC.Api.dll"]
```

#### Kubernetes Configuration
```yaml
apiVersion: v1
kind: Service
metadata:
  name: remotec-api
spec:
  type: LoadBalancer
  sessionAffinity: ClientIP  # Sticky sessions for SignalR
  sessionAffinityConfig:
    clientIP:
      timeoutSeconds: 3600
```

#### Monitoring and Metrics
- Application Insights for real-time monitoring
- Custom performance counters
- Distributed tracing with OpenTelemetry
- Alert on performance degradation

## Performance Testing

### Running Performance Tests

```bash
# Linux/Mac
cd tests/RemoteC.Tests.Performance
./run-performance-tests.sh

# Windows
cd tests\RemoteC.Tests.Performance
run-performance-tests.bat
```

### Analyzing Results
1. Check `results/[timestamp]/benchmark_results.txt` for detailed metrics
2. Review `results/[timestamp]/summary_report.md` for analysis
3. Compare with previous runs to track improvements
4. Identify bottlenecks and prioritize optimizations

## Monitoring Production Performance

### Key Metrics to Track
- **P50/P95/P99 Response Times**: API endpoint latencies
- **Frame Rate**: Actual FPS delivered to clients
- **CPU/Memory Usage**: Resource consumption patterns
- **Network Throughput**: Bandwidth utilization
- **Error Rates**: Failed requests and disconnections

### Performance Dashboards
```csharp
// Custom metrics collection
public class PerformanceMetrics
{
    private readonly IMetricsCollector _metrics;
    
    public async Task RecordScreenCapture(double latencyMs)
    {
        await _metrics.RecordValue("screen_capture_latency", latencyMs);
        
        if (latencyMs > 100) // Phase 1 target
        {
            await _metrics.RecordEvent("screen_capture_slow", new 
            {
                latency = latencyMs,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
```

## Optimization Checklist

### Before Deployment
- [ ] Run performance benchmarks
- [ ] Profile database queries
- [ ] Analyze memory usage
- [ ] Test under load (1000+ concurrent users)
- [ ] Verify caching effectiveness

### After Deployment
- [ ] Monitor real-world performance
- [ ] Analyze user experience metrics
- [ ] Review error logs for performance issues
- [ ] Conduct regular performance reviews
- [ ] Update optimization strategies based on data

## Future Enhancements

### Machine Learning Optimization
- Predictive frame pre-fetching
- Intelligent quality adjustment
- Anomaly detection for performance issues

### Edge Computing
- Deploy capture agents closer to users
- Reduce round-trip latency
- Implement regional failover

### Advanced Compression
- AI-powered compression algorithms
- Context-aware encoding
- Perceptual quality optimization