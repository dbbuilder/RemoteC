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

            _logger.LogInformation("Generated PIN for session {SessionId}", sessionId);

            return pin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PIN for session {SessionId}", sessionId);
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
                _logger.LogWarning("PIN validation failed: No PIN found for session {SessionId}", sessionId);
                return false;
            }

            var pinData = JsonSerializer.Deserialize<dynamic>(cachedData);
            var hashedPin = pinData?.GetProperty("HashedPin").GetString();
            var isUsed = pinData?.GetProperty("IsUsed").GetBoolean() ?? false;

            if (isUsed)
            {
                _logger.LogWarning("PIN validation failed: PIN already used for session {SessionId}", sessionId);
                return false;
            }

            // Verify PIN
            var isValid = VerifyPin(pin, hashedPin);

            if (isValid)
            {
                // Mark PIN as used
                await InvalidatePinAsync(sessionId);
                _logger.LogInformation("PIN validation successful for session {SessionId}", sessionId);
            }
            else
            {
                _logger.LogWarning("PIN validation failed: Invalid PIN for session {SessionId}", sessionId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN for session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task InvalidatePinAsync(Guid sessionId)
    {
        try
        {
            var cacheKey = GetPinCacheKey(sessionId);
            await _cache.RemoveAsync(cacheKey);

            _logger.LogInformation("Invalidated PIN for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating PIN for session {SessionId}", sessionId);
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
            _logger.LogError(ex, "Error checking PIN validity for session {SessionId}", sessionId);
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
            _logger.LogInformation("Starting remote session {SessionId} for device {DeviceId}", sessionId, deviceId);

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

            _logger.LogInformation("Remote session {SessionId} started successfully", sessionId);

            return connectionUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting remote session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task StopRemoteSessionAsync(Guid sessionId)
    {
        try
        {
            _logger.LogInformation("Stopping remote session {SessionId}", sessionId);

            var httpClient = _httpClientFactory.CreateClient("ControlR");
            var apiUrl = _configuration["RemoteControl:ControlR:ApiUrl"];
            var apiKey = _configuration["RemoteControl:ControlR:ApiKey"];

            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await httpClient.DeleteAsync($"{apiUrl}/api/sessions/{sessionId}");
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Remote session {SessionId} stopped successfully", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping remote session {SessionId}", sessionId);
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
            _logger.LogError(ex, "Error sending input to session {SessionId}", sessionId);
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
            _logger.LogError(ex, "Error getting screenshot for session {SessionId}", sessionId);
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
            _logger.LogError(ex, "Error checking session status for {SessionId}", sessionId);
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
            _logger.LogInformation("Executing command for session {SessionId}: {Command}", sessionId, command);

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

            _logger.LogInformation("Command executed for session {SessionId}. Success: {Success}, ExitCode: {ExitCode}", 
                sessionId, result.Success, result.ExitCode);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command for session {SessionId}", sessionId);
            
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
            _logger.LogError(ex, "Error checking if command is allowed: {Command}", command);
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

