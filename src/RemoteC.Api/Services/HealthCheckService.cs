using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using RemoteC.Data;
using StackExchange.Redis;

namespace RemoteC.Api.Services
{
    /// <summary>
    /// Custom health check for database connectivity
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly RemoteCDbContext _context;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(RemoteCDbContext context, ILogger<DatabaseHealthCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Test database connectivity
                await _context.Database.CanConnectAsync(cancellationToken);
                
                // Optionally run a simple query
                var userCount = await _context.Users.CountAsync(cancellationToken);
                
                return HealthCheckResult.Healthy($"Database is healthy. User count: {userCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy("Database connection failed", ex);
            }
        }
    }

    /// <summary>
    /// Custom health check for Redis connectivity
    /// </summary>
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisHealthCheck> _logger;

        public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisHealthCheck> logger)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var database = _connectionMultiplexer.GetDatabase();
                await database.PingAsync();
                
                var endpoints = _connectionMultiplexer.GetEndPoints();
                var server = _connectionMultiplexer.GetServer(endpoints[0]);
                var info = await server.InfoAsync();
                
                return HealthCheckResult.Healthy($"Redis is healthy. Connected to {endpoints.Length} endpoint(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return HealthCheckResult.Unhealthy("Redis connection failed", ex);
            }
        }
    }

    /// <summary>
    /// Custom health check for external service dependencies
    /// </summary>
    public class ExternalServicesHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ExternalServicesHealthCheck> _logger;

        public ExternalServicesHealthCheck(IHttpClientFactory httpClientFactory, ILogger<ExternalServicesHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check Azure AD B2C endpoint (metadata endpoint)
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                // This is a public metadata endpoint that should always be available
                var response = await client.GetAsync("https://login.microsoftonline.com/common/.well-known/openid-configuration", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("External services are reachable");
                }
                else
                {
                    return HealthCheckResult.Degraded($"External service returned {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External services health check failed");
                return HealthCheckResult.Unhealthy("External services unreachable", ex);
            }
        }
    }

    /// <summary>
    /// Custom health check for disk space
    /// </summary>
    public class DiskSpaceHealthCheck : IHealthCheck
    {
        private readonly long _minimumFreeMegabytes;
        private readonly ILogger<DiskSpaceHealthCheck> _logger;

        public DiskSpaceHealthCheck(long minimumFreeMegabytes, ILogger<DiskSpaceHealthCheck> logger)
        {
            _minimumFreeMegabytes = minimumFreeMegabytes;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var drives = DriveInfo.GetDrives();
                var appDrive = drives.FirstOrDefault(d => d.Name == Path.GetPathRoot(AppContext.BaseDirectory));
                
                if (appDrive == null || !appDrive.IsReady)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("Cannot access drive information"));
                }
                
                var freeSpaceMb = appDrive.AvailableFreeSpace / (1024 * 1024);
                var totalSpaceMb = appDrive.TotalSize / (1024 * 1024);
                var usedPercentage = ((totalSpaceMb - freeSpaceMb) * 100) / totalSpaceMb;
                
                if (freeSpaceMb < _minimumFreeMegabytes)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        $"Insufficient disk space. Free: {freeSpaceMb}MB, Required: {_minimumFreeMegabytes}MB"));
                }
                
                if (usedPercentage > 90)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Disk space is low. Used: {usedPercentage}%, Free: {freeSpaceMb}MB"));
                }
                
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Disk space is healthy. Free: {freeSpaceMb}MB ({100 - usedPercentage}%)"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Disk space health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy("Cannot check disk space", ex));
            }
        }
    }
}