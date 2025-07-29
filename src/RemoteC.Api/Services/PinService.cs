using RemoteC.Shared.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace RemoteC.Api.Services;

/// <summary>
/// PIN code service implementation
/// </summary>
public class PinService : IPinService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<PinService> _logger;
    private readonly IConfiguration _configuration;

    public PinService(
        IDistributedCache cache,
        ILogger<PinService> logger,
        IConfiguration configuration)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<string> GeneratePinAsync(Guid sessionId)
    {
        try
        {
            // Generate random PIN
            var pinLength = _configuration.GetValue<int>("Security:PinLength", 6);
            var pin = GenerateRandomPin(pinLength);

            // Hash the PIN for storage
            var hashedPin = HashPin(pin);

            // Store in cache with expiration
            var expirationMinutes = _configuration.GetValue<int>("Security:PinExpirationMinutes", 10);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes)
            };

            var pinData = new
            {
                HashedPin = hashedPin,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                IsUsed = false
            };

            var cacheKey = GetPinCacheKey(sessionId);
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(pinData), options);

            _logger.Information("Generated PIN for session {SessionId}", sessionId);

            return pin;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating PIN for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> ValidatePinAsync(Guid sessionId, string pin)
    {
        try
        {
            var cacheKey = GetPinCacheKey(sessionId);
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.Warning("PIN validation failed: No PIN found for session {SessionId}", sessionId);
                return false;
            }

            var pinData = JsonSerializer.Deserialize<dynamic>(cachedData);
            var hashedPin = pinData?.GetProperty("HashedPin").GetString();
            var isUsed = pinData?.GetProperty("IsUsed").GetBoolean() ?? false;

            if (isUsed)
            {
                _logger.Warning("PIN validation failed: PIN already used for session {SessionId}", sessionId);
                return false;
            }

            // Verify PIN
            var isValid = VerifyPin(pin, hashedPin);

            if (isValid)
            {
                // Mark PIN as used
                await InvalidatePinAsync(sessionId);
                _logger.Information("PIN validation successful for session {SessionId}", sessionId);
            }
            else
            {
                _logger.Warning("PIN validation failed: Invalid PIN for session {SessionId}", sessionId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error validating PIN for session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task InvalidatePinAsync(Guid sessionId)
    {
        try
        {
            var cacheKey = GetPinCacheKey(sessionId);
            await _cache.RemoveAsync(cacheKey);

            _logger.Information("Invalidated PIN for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error invalidating PIN for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> IsPinValidAsync(Guid sessionId, string pin)
    {
        try
        {
            var cacheKey = GetPinCacheKey(sessionId);
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(cachedData))
            {
                return false;
            }

            var pinData = JsonSerializer.Deserialize<dynamic>(cachedData);
            var hashedPin = pinData?.GetProperty("HashedPin").GetString();
            var isUsed = pinData?.GetProperty("IsUsed").GetBoolean() ?? false;

            if (isUsed)
            {
                return false;
            }

            return VerifyPin(pin, hashedPin);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking PIN validity for session {SessionId}", sessionId);
            return false;
        }
    }

    private static string GenerateRandomPin(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);

        var pin = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            pin.Append((bytes[i] % 10).ToString());
        }

        return pin.ToString();
    }

    private static string HashPin(string pin)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pin));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPin(string pin, string? hashedPin)
    {
        if (string.IsNullOrEmpty(hashedPin))
            return false;

        var computedHash = HashPin(pin);
        return computedHash == hashedPin;
    }

    private static string GetPinCacheKey(Guid sessionId)
    {
        return $"pin:session:{sessionId}";
    }
}

/// <summary>
/// Remote control service implementation using ControlR
/// </summary>
public class RemoteControlService : IRemoteControlService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RemoteControlService> _logger;
    private readonly IConfiguration _configuration;

    public RemoteControlService(
        IHttpClientFactory httpClientFactory,
        ILogger<RemoteControlService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<string> StartRemoteSessionAsync(Guid sessionId, string deviceId)
    {
        try
        {
            _logger.Information("Starting remote session {SessionId} for device {DeviceId}", sessionId, deviceId);

            var httpClient = _httpClientFactory.CreateClient("ControlR");
            var apiUrl = _configuration["RemoteControl:ControlR:ApiUrl"];
            var apiKey = _configuration["RemoteControl:ControlR:ApiKey"];

            var request = new
            {
                SessionId = sessionId,
                DeviceId = deviceId,
                Timestamp = DateTime.UtcNow
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await httpClient.PostAsync($"{apiUrl}/api/sessions/start", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<dynamic>(responseContent);
            
            var connectionUrl = result?.GetProperty("connectionUrl").GetString() ?? string.Empty;

            _logger.Information("Remote session {SessionId} started successfully", sessionId);

            return connectionUrl;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error starting remote session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task StopRemoteSessionAsync(Guid sessionId)
    {
        try
        {
            _logger.Information("Stopping remote session {SessionId}", sessionId);

            var httpClient = _httpClientFactory.CreateClient("ControlR");
            var apiUrl = _configuration["RemoteControl:ControlR:ApiUrl"];
            var apiKey = _configuration["RemoteControl:ControlR:ApiKey"];

            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await httpClient.DeleteAsync($"{apiUrl}/api/sessions/{sessionId}");
            response.EnsureSuccessStatusCode();

            _logger.Information("Remote session {SessionId} stopped successfully", sessionId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error stopping remote session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> SendInputAsync(Guid sessionId, object inputData)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("ControlR");
            var apiUrl = _configuration["RemoteControl:ControlR:ApiUrl"];
            var apiKey = _configuration["RemoteControl:ControlR:ApiKey"];

            var content = new StringContent(JsonSerializer.Serialize(inputData), Encoding.UTF8, "application/json");
            
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await httpClient.PostAsync($"{apiUrl}/api/sessions/{sessionId}/input", content);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error sending input to session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<byte[]> GetScreenshotAsync(Guid sessionId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("ControlR");
            var apiUrl = _configuration["RemoteControl:ControlR:ApiUrl"];
            var apiKey = _configuration["RemoteControl:ControlR:ApiKey"];

            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await httpClient.GetAsync($"{apiUrl}/api/sessions/{sessionId}/screenshot");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting screenshot for session {SessionId}", sessionId);
            return Array.Empty<byte>();
        }
    }

    public async Task<bool> IsSessionActiveAsync(Guid sessionId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("ControlR");
            var apiUrl = _configuration["RemoteControl:ControlR:ApiUrl"];
            var apiKey = _configuration["RemoteControl:ControlR:ApiKey"];

            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await httpClient.GetAsync($"{apiUrl}/api/sessions/{sessionId}/status");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<dynamic>(content);
                return result?.GetProperty("isActive").GetBoolean() ?? false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking session status for {SessionId}", sessionId);
            return false;
        }
    }
}Async(u => u.Id.ToString() == userId);

            if (user == null)
            {
                throw new ArgumentException($"User {userId} not found");
            }

            // Update user properties
            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;

            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;

            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;

            // Update roles if provided
            if (request.Roles != null)
            {
                // Remove existing roles
                _context.UserRoles.RemoveRange(user.UserRoles);

                // Add new roles
                foreach (var roleName in request.Roles)
                {
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                    if (role != null)
                    {
                        var userRole = new UserRole
                        {
                            UserId = user.Id,
                            RoleId = role.Id,
                            AssignedBy = user.Id
                        };
                        _context.UserRoles.Add(userRole);
                    }
                }
            }

            await _context.SaveChangesAsync();

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = request.Roles ?? new List<string>();

            _logger.Information("User {UserId} updated successfully", userId);

            return userDto;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId)
    {
        try
        {
            var permissions = await _context.Users
                .Where(u => u.Id.ToString() == userId)
                .SelectMany(u => u.UserRoles)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting permissions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        try
        {
            var hasPermission = await _context.Users
                .Where(u => u.Id.ToString() == userId)
                .SelectMany(u => u.UserRoles)
                .SelectMany(ur => ur.Role.RolePermissions)
                .AnyAsync(rp => rp.Permission.Name == permission);

            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
            return false;
        }
    }
}

/// <summary>
/// Command execution service implementation
/// </summary>
public class CommandExecutionService : ICommandExecutionService
{
    private readonly ILogger<CommandExecutionService> _logger;
    private readonly IConfiguration _configuration;

    public CommandExecutionService(
        ILogger<CommandExecutionService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<CommandExecutionResult> ExecuteCommandAsync(Guid sessionId, string command, string shell = "powershell")
    {
        try
        {
            _logger.Information("Executing command for session {SessionId}: {Command}", sessionId, command);

            // Validate command is allowed
            if (!await IsCommandAllowedAsync(command))
            {
                throw new UnauthorizedAccessException($"Command '{command}' is not allowed");
            }

            var startTime = DateTime.UtcNow;
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = GetShellExecutable(shell),
                Arguments = GetShellArguments(shell, command),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with timeout
            var timeoutMs = _configuration.GetValue<int>("CommandExecution:TimeoutMs", 30000);
            var completed = process.WaitForExit(timeoutMs);

            if (!completed)
            {
                process.Kill();
                throw new TimeoutException($"Command execution timed out after {timeoutMs}ms");
            }

            var endTime = DateTime.UtcNow;
            var executionTime = endTime - startTime;

            var result = new CommandExecutionResult
            {
                Success = process.ExitCode == 0,
                Output = outputBuilder.ToString(),
                ErrorOutput = errorBuilder.ToString(),
                ExitCode = process.ExitCode,
                ExecutionTime = executionTime,
                ExecutedAt = startTime
            };

            _logger.Information("Command executed for session {SessionId}. Success: {Success}, ExitCode: {ExitCode}", 
                sessionId, result.Success, result.ExitCode);

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing command for session {SessionId}", sessionId);
            
            return new CommandExecutionResult
            {
                Success = false,
                ErrorOutput = ex.Message,
                ExitCode = -1,
                ExecutionTime = TimeSpan.Zero,
                ExecutedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<IEnumerable<CommandHistoryDto>> GetCommandHistoryAsync(Guid sessionId)
    {
        // TODO: Implement command history storage and retrieval
        await Task.CompletedTask;
        return new List<CommandHistoryDto>();
    }

    public async Task<bool> IsCommandAllowedAsync(string command)
    {
        try
        {
            // Get allowed and blocked commands from configuration
            var allowedCommands = _configuration.GetSection("CommandExecution:AllowedCommands").Get<string[]>() ?? Array.Empty<string>();
            var blockedCommands = _configuration.GetSection("CommandExecution:BlockedCommands").Get<string[]>() ?? Array.Empty<string>();

            var commandLower = command.ToLowerInvariant().Trim();

            // Check if command is explicitly blocked
            if (blockedCommands.Any(blocked => commandLower.Contains(blocked.ToLowerInvariant())))
            {
                return false;
            }

            // If allow list is configured, check if command is allowed
            if (allowedCommands.Length > 0)
            {
                return allowedCommands.Any(allowed => commandLower.StartsWith(allowed.ToLowerInvariant()));
            }

            // Default security policy - block dangerous commands
            var dangerousCommands = new[]
            {
                "format", "del", "rm", "rmdir", "rd", "erase",
                "shutdown", "restart", "reboot", "halt",
                "diskpart", "fdisk", "mkfs",
                "net user", "useradd", "userdel",
                "reg delete", "regedit",
                "sc delete", "sc stop",
                "taskkill", "kill"
            };

            return !dangerousCommands.Any(dangerous => commandLower.Contains(dangerous));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking if command is allowed: {Command}", command);
            return false;
        }
    }

    private static string GetShellExecutable(string shell)
    {
        return shell.ToLowerInvariant() switch
        {
            "powershell" => "powershell.exe",
            "cmd" => "cmd.exe",
            "bash" => "bash",
            "sh" => "sh",
            _ => "powershell.exe"
        };
    }

    private static string GetShellArguments(string shell, string command)
    {
        return shell.ToLowerInvariant() switch
        {
            "powershell" => $"-Command \"{command}\"",
            "cmd" => $"/C \"{command}\"",
            "bash" or "sh" => $"-c \"{command}\"",
            _ => $"-Command \"{command}\""
        };
    }
}

/// <summary>
/// Audit service implementation
/// </summary>
public class AuditService : IAuditService
{
    private readonly RemoteCDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        RemoteCDbContext context,
        ILogger<AuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task LogActionAsync(string action, string? entityType = null, string? entityId = null, 
        string? userId = null, object? oldValues = null, object? newValues = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId ?? httpContext?.User?.Identity?.Name,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request?.Headers?["User-Agent"].ToString(),
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                Timestamp = DateTime.UtcNow,
                Success = true
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.Debug("Audit log created for action {Action}", action);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating audit log for action {Action}", action);
            // Don't throw - audit failures shouldn't break application flow
        }
    }

    public async Task LogSecurityEventAsync(string eventType, string userId, string? details = null)
    {
        try
        {
            await LogActionAsync($"security.{eventType}", "Security", null, userId, null, new { details });
            _logger.Information("Security event logged: {EventType} for user {UserId}", eventType, userId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error logging security event {EventType}", eventType);
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(DateTime? from = null, DateTime? to = null, 
        string? userId = null, string? action = null)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (from.HasValue)
                query = query.Where(al => al.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(al => al.Timestamp <= to.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(al => al.UserId == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(al => al.Action.Contains(action));

            var auditLogs = await query
                .OrderByDescending(al => al.Timestamp)
                .Take(1000) // Limit results for performance
                .ToListAsync();

            return auditLogs.Select(al => new AuditLogDto
            {
                Id = al.Id,
                Action = al.Action,
                EntityType = al.EntityType,
                EntityId = al.EntityId,
                UserId = al.UserId,
                IpAddress = al.IpAddress,
                Timestamp = al.Timestamp,
                Success = al.Success,
                ErrorMessage = al.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting audit logs");
            throw;
        }
    }
}