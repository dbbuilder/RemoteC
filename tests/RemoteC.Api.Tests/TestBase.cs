using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace RemoteC.Api.Tests
{
    /// <summary>
    /// Base class for all tests with common setup
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected readonly IServiceCollection Services;
        protected readonly IServiceProvider ServiceProvider;
        protected readonly IConfiguration Configuration;
        protected readonly Mock<ILogger> LoggerMock;

        protected TestBase()
        {
            Services = new ServiceCollection();
            
            // Setup configuration
            Configuration = CreateTestConfiguration();
            Services.AddSingleton(Configuration);
            
            // Setup logging
            LoggerMock = new Mock<ILogger>();
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(LoggerMock.Object);
            Services.AddSingleton(loggerFactory.Object);
            
            // Add other common services
            ConfigureServices(Services);
            
            ServiceProvider = Services.BuildServiceProvider();
        }

        /// <summary>
        /// Override to add additional services
        /// </summary>
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Override in derived classes
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (ServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private static IConfiguration CreateTestConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                // Security settings
                { "Security:PinLength", "6" },
                { "Security:PinExpirationMinutes", "10" },
                { "Security:MaxLoginAttempts", "5" },
                { "Security:LockoutDurationMinutes", "15" },
                
                // JWT settings
                { "Jwt:Key", "TestSecretKeyForDevelopmentOnly1234567890" },
                { "Jwt:Issuer", "RemoteC.Test" },
                { "Jwt:Audience", "RemoteC.Test" },
                { "Jwt:ExpirationMinutes", "60" },
                
                // Redis settings
                { "Redis:ConnectionString", "localhost:6379" },
                { "Redis:InstanceName", "RemoteCTest" },
                
                // Database settings
                { "ConnectionStrings:DefaultConnection", "Server=(localdb)\\mssqllocaldb;Database=RemoteCTest;Trusted_Connection=True;" },
                
                // Application settings
                { "Application:Name", "RemoteC Test" },
                { "Application:Version", "1.0.0" },
                { "Application:Environment", "Test" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }
    }
}
