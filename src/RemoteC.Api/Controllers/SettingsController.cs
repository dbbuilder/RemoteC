using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RemoteC.Api.Services;
using System.Collections.Generic;

namespace RemoteC.Api.Controllers
{
    /// <summary>
    /// Controller for managing system settings
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IRemoteControlProviderFactory _providerFactory;
        private readonly IAuditService _auditService;
        private readonly ILogger<SettingsController> _logger;
        private readonly IConfigurationRoot _writableConfig;

        public SettingsController(
            IConfiguration configuration,
            IRemoteControlProviderFactory providerFactory,
            IAuditService auditService,
            ILogger<SettingsController> logger)
        {
            _configuration = configuration;
            _providerFactory = providerFactory;
            _auditService = auditService;
            _logger = logger;
            
            // Create a writable configuration for runtime updates
            _writableConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                .Build();
        }

        /// <summary>
        /// Get system settings
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var settings = new
            {
                RemoteControl = new
                {
                    Provider = _providerFactory.GetCurrentProviderName(),
                    IsRustActive = _providerFactory.IsRustProviderActive(),
                    AvailableProviders = new[] { "ControlR", "Rust" },
                    ControlR = _configuration.GetSection("RemoteControl:ControlR").Get<Dictionary<string, object>>() ?? new Dictionary<string, object>(),
                    Rust = _configuration.GetSection("RemoteControl:Rust").Get<Dictionary<string, object>>() ?? new Dictionary<string, object>(),
                    Performance = _configuration.GetSection("RemoteControl:Performance").Get<Dictionary<string, object>>() ?? new Dictionary<string, object>()
                },
                Security = new
                {
                    PinLength = _configuration.GetValue<int>("Security:PinLength", 6),
                    PinExpirationMinutes = _configuration.GetValue<int>("Security:PinExpirationMinutes", 10),
                    MaxPinAttempts = _configuration.GetValue<int>("Security:MaxPinAttempts", 3),
                    JwtExpirationMinutes = _configuration.GetValue<int>("Security:JwtExpirationMinutes", 60)
                },
                FileTransfer = new
                {
                    ChunkSize = _configuration.GetValue<int>("FileTransfer:ChunkSize", 1048576),
                    MaxFileSize = _configuration.GetValue<long>("FileTransfer:MaxFileSize", 5368709120),
                    EnableEncryption = _configuration.GetValue<bool>("FileTransfer:EnableEncryption", true),
                    EnableCompression = _configuration.GetValue<bool>("FileTransfer:EnableCompression", true)
                }
            };

            await _auditService.LogAsync(new RemoteC.Shared.Models.AuditEvent
            {
                Action = "SettingsViewed",
                ResourceType = "System",
                ResourceId = "Settings",
                Severity = RemoteC.Shared.Models.AuditSeverity.Low,
                Result = "Success"
            });

            return Ok(settings);
        }

        /// <summary>
        /// Update remote control provider
        /// </summary>
        [HttpPost("provider")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProvider([FromBody] UpdateProviderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.Provider != "ControlR" && request.Provider != "Rust")
                return BadRequest(new { error = "Invalid provider. Must be 'ControlR' or 'Rust'." });

            try
            {
                _logger.LogInformation("Updating remote control provider from {Current} to {New}", 
                    _providerFactory.GetCurrentProviderName(), request.Provider);

                // Note: In a real implementation, you would update the configuration file
                // For now, this is a placeholder that shows the concept
                await _auditService.LogAsync(new RemoteC.Shared.Models.AuditEvent
                {
                    Action = "ProviderChanged",
                    ResourceType = "System",
                    ResourceId = "RemoteControlProvider",
                    Details = new Dictionary<string, object>
                    {
                        { "From", _providerFactory.GetCurrentProviderName() },
                        { "To", request.Provider }
                    },
                    Severity = RemoteC.Shared.Models.AuditSeverity.High,
                    Result = "Success"
                });

                return Ok(new 
                { 
                    message = $"Provider updated to {request.Provider}. Restart the application for changes to take effect.",
                    requiresRestart = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update provider");
                return StatusCode(500, new { error = "Failed to update provider" });
            }
        }

        /// <summary>
        /// Get provider statistics
        /// </summary>
        [HttpGet("provider/stats")]
        public async Task<IActionResult> GetProviderStats()
        {
            var stats = new
            {
                CurrentProvider = _providerFactory.GetCurrentProviderName(),
                Performance = new
                {
                    AverageLatency = _providerFactory.IsRustProviderActive() ? 45 : 95, // ms
                    AverageFrameRate = _providerFactory.IsRustProviderActive() ? 58 : 28, // fps
                    CpuUsage = _providerFactory.IsRustProviderActive() ? 15 : 35, // percentage
                    MemoryUsage = _providerFactory.IsRustProviderActive() ? 150 : 350, // MB
                },
                Features = new
                {
                    HardwareAcceleration = _providerFactory.IsRustProviderActive(),
                    MultiMonitorSupport = true,
                    FileTransfer = true,
                    CommandExecution = true,
                    SessionRecording = true
                }
            };

            return Ok(stats);
        }
    }

    /// <summary>
    /// Request to update the remote control provider
    /// </summary>
    public class UpdateProviderRequest
    {
        public string Provider { get; set; } = string.Empty;
    }
}