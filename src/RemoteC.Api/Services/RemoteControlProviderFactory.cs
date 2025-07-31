using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RemoteC.Core.Interop;
using RemoteC.Host.Services;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    /// <summary>
    /// Factory for creating remote control providers based on configuration
    /// </summary>
    public interface IRemoteControlProviderFactory
    {
        IRemoteControlProvider CreateProvider();
        string GetCurrentProviderName();
        bool IsRustProviderActive();
    }

    public class RemoteControlProviderFactory : IRemoteControlProviderFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RemoteControlProviderFactory> _logger;
        private readonly string _providerName;

        public RemoteControlProviderFactory(
            IConfiguration configuration,
            ILogger<RemoteControlProviderFactory> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _providerName = _configuration["RemoteControl:Provider"] ?? "ControlR";
        }

        public IRemoteControlProvider CreateProvider()
        {
            _logger.LogInformation("Creating remote control provider: {Provider}", _providerName);

            switch (_providerName.ToLowerInvariant())
            {
                case "rust":
                    return CreateRustProvider();
                
                case "controlr":
                    return CreateControlRProvider();
                
                default:
                    _logger.LogWarning("Unknown provider '{Provider}', defaulting to ControlR", _providerName);
                    return CreateControlRProvider();
            }
        }

        public string GetCurrentProviderName() => _providerName;

        public bool IsRustProviderActive() => _providerName.Equals("Rust", StringComparison.OrdinalIgnoreCase);

        private IRemoteControlProvider CreateRustProvider()
        {
            try
            {
                _logger.LogInformation("Initializing Rust remote control provider");
                
                var rustConfig = _configuration.GetSection("RemoteControl:Rust");
                var provider = new RustRemoteControlProvider();
                
                // Apply configuration
                // TODO: Pass configuration to Rust provider
                
                _logger.LogInformation("Rust provider created successfully");
                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Rust provider, falling back to ControlR");
                return CreateControlRProvider();
            }
        }

        private IRemoteControlProvider CreateControlRProvider()
        {
            _logger.LogInformation("Initializing ControlR remote control provider");
            
            var controlRConfig = _configuration.GetSection("RemoteControl:ControlR");
            
            // Create ControlR provider with configuration
            var provider = new ControlRProvider(
                controlRConfig["ApiUrl"] ?? "https://localhost:5000",
                controlRConfig["ApiKey"] ?? string.Empty,
                controlRConfig.GetValue<bool>("EnableLogging", true),
                controlRConfig.GetValue<int>("ConnectionTimeoutMs", 30000)
            );
            
            _logger.LogInformation("ControlR provider created successfully");
            return provider;
        }
    }

    /// <summary>
    /// Configuration options for remote control providers
    /// </summary>
    public class RemoteControlOptions
    {
        public string Provider { get; set; } = "ControlR";
        public ControlROptions ControlR { get; set; } = new();
        public RustProviderOptions Rust { get; set; } = new();
        public PerformanceOptions Performance { get; set; } = new();
    }

    public class ControlROptions
    {
        public string ApiUrl { get; set; } = "https://localhost:5000";
        public string ApiKey { get; set; } = string.Empty;
        public bool EnableLogging { get; set; } = true;
        public int ConnectionTimeoutMs { get; set; } = 30000;
    }

    public class RustProviderOptions
    {
        public bool EnableHardwareAcceleration { get; set; } = true;
        public int TargetFrameRate { get; set; } = 60;
        public string CaptureMode { get; set; } = "PrimaryMonitor";
        public string TransportProtocol { get; set; } = "Quic";
        public bool EnableFrameDifferencing { get; set; } = true;
        public string CompressionLevel { get; set; } = "Medium";
    }

    public class PerformanceOptions
    {
        public int MaxConcurrentSessions { get; set; } = 100;
        public int SessionTimeoutMinutes { get; set; } = 60;
        public int MaxFrameRate { get; set; } = 60;
        public int DefaultQuality { get; set; } = 75;
    }
}