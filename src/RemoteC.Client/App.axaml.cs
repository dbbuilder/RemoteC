using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RemoteC.Client.Services;
using RemoteC.Client.ViewModels;
using RemoteC.Client.Views;
using Serilog;

namespace RemoteC.Client
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        public static IConfiguration Configuration { get; private set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Load configuration
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .Build();

            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainWindowViewModel>(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Configuration
            services.AddSingleton(Configuration);

            // Services
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<IRemoteControlService, RemoteControlService>();
            services.AddSingleton<ISessionService, SessionService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IFileTransferService, FileTransferService>();
            services.AddSingleton<IClipboardService, ClipboardService>();
            
            // SignalR
            services.AddSingleton<ISignalRService, SignalRService>();
            
            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<ConnectViewModel>();
            services.AddTransient<SessionViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<FileTransferViewModel>();
            
            // Logging
            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));
        }
    }
}