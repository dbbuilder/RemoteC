using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using RemoteC.Host.Services;
using RemoteC.Shared.Models;
using Serilog;
using Serilog.Events;

namespace RemoteC.Host;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                    "RemoteC", "Host", "logs", "remotec-host-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        try
        {
            Log.Information("Starting RemoteC Host Service");
            
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "RemoteC Host Service terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseWindowsService(options =>
            {
                options.ServiceName = "RemoteC Host Service";
            })
            .UseSerilog()
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                // Add command line configuration source
                config.AddCommandLine(args, new Dictionary<string, string>
                {
                    { "--host-id", "Host:Id" },
                    { "--host-secret", "Host:Secret" },
                    { "--server", "Api:BaseUrl" },
                    { "--token-endpoint", "Api:TokenEndpoint" },
                    { "--id", "Host:Id" }, // Short form
                    { "--secret", "Host:Secret" }, // Short form
                    { "-s", "Api:BaseUrl" }, // Short form
                });
            })
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                // HTTP client
                services.AddHttpClient();
                
                // Core services
                services.AddSingleton<IScreenCaptureService, ScreenCaptureService>();
                services.AddSingleton<IInputControlService, InputControlService>();
                services.AddSingleton<ISystemInfoService, SystemInfoService>();
                services.AddSingleton<IPerformanceMonitorService, PerformanceMonitorService>();
                services.AddSingleton<IFileSystemService, FileSystemService>();
                services.AddSingleton<IProcessManagementService, ProcessManagementService>();
                services.AddSingleton<IClipboardService, ClipboardService>();
                services.AddSingleton<IAudioService, AudioService>();
                
                // Platform-specific services
                services.AddSingleton<IClipboardAccess>(sp =>
                {
                    var logger = sp.GetService<ILogger<IClipboardAccess>>();
                    return ClipboardAccessFactory.Create(logger);
                });
                services.AddSingleton<IClipboardManager, ClipboardManager>();
                
                // Remote control provider
                services.AddSingleton<IRemoteControlProvider>(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var logger = sp.GetRequiredService<ILogger<Program>>();
                    var providerType = config["RemoteControlProvider:Type"];
                    var fallbackToStub = config.GetValue<bool>("RemoteControlProvider:Settings:FallbackToStub", false);
                    
                    try
                    {
                        return providerType switch
                        {
                            "ControlR" => new ControlRProvider(config), // Phase 1
                            "Rust" => new RemoteC.Core.Interop.RustRemoteControlProvider(), // Phase 2
                            "Stub" => new StubRemoteControlProvider(sp.GetService<ILogger<StubRemoteControlProvider>>()), // Development
                            _ => throw new InvalidOperationException($"Unknown provider type: {providerType}")
                        };
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, $"Failed to initialize {providerType} provider");
                        
                        if (fallbackToStub)
                        {
                            logger.LogInformation("Falling back to Stub provider for development");
                            return new StubRemoteControlProvider(sp.GetService<ILogger<StubRemoteControlProvider>>());
                        }
                        
                        throw;
                    }
                });
                
                // Communication services
                services.AddSingleton<ISignalRService, SignalRService>();
                services.AddSingleton<IConnectionManager, ConnectionManager>();
                services.AddSingleton<ICommandExecutor, CommandExecutor>();
                services.AddSingleton<ISessionManager, SessionManager>();
                
                // Security services
                services.AddSingleton<IAuthenticationService, AuthenticationService>();
                services.AddSingleton<IEncryptionService, EncryptionService>();
                services.AddSingleton<IPermissionService, PermissionService>();
                
                // Configuration
                services.Configure<HostConfiguration>(configuration.GetSection("HostConfiguration"));
                
                // Background services
                services.AddHostedService<RemoteControlHostService>();
                services.AddHostedService<HealthCheckService>();
                services.AddHostedService<MetricsReportingService>();
            });
}