using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RemoteC.Api;
using RemoteC.Data;
using StackExchange.Redis;
using Xunit;

namespace RemoteC.Tests.Integration.TestFixtures
{
    /// <summary>
    /// Base class for integration tests that provides common functionality
    /// </summary>
    public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        protected readonly WebApplicationFactory<Program> Factory;
        protected HttpClient Client { get; private set; } = null!;
        protected IServiceProvider Services { get; private set; } = null!;
        protected DatabaseFixture? DatabaseFixture { get; set; }
        protected RedisFixture? RedisFixture { get; set; }

        protected IntegrationTestBase(WebApplicationFactory<Program> factory)
        {
            Factory = factory;
        }

        public virtual async Task InitializeAsync()
        {
            // Initialize test containers if needed
            if (UsesDatabase)
            {
                DatabaseFixture = new DatabaseFixture();
                await DatabaseFixture.InitializeAsync();
            }

            if (UsesRedis)
            {
                RedisFixture = new RedisFixture();
                await RedisFixture.InitializeAsync();
            }

            // Configure the test server
            var builder = Factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace database with test container instance
                    if (DatabaseFixture != null)
                    {
                        services.RemoveAll<RemoteCDbContext>();
                        services.AddDbContext<RemoteCDbContext>(options =>
                        {
                            options.UseSqlServer(DatabaseFixture.ConnectionString);
                        });
                    }

                    // Replace Redis with test container instance
                    if (RedisFixture != null)
                    {
                        services.RemoveAll<IConnectionMultiplexer>();
                        services.AddSingleton(RedisFixture.ConnectionMultiplexer!);
                    }

                    // Add any test-specific services
                    ConfigureTestServices(services);
                });
            });

            Services = builder.Services;
            Client = builder.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Set up authentication if needed
            if (RequiresAuthentication)
            {
                await AuthenticateAsync();
            }
        }

        public virtual async Task DisposeAsync()
        {
            Client?.Dispose();
            
            if (DatabaseFixture != null)
            {
                await DatabaseFixture.DisposeAsync();
            }

            if (RedisFixture != null)
            {
                await RedisFixture.DisposeAsync();
            }
        }

        /// <summary>
        /// Override to configure additional test services
        /// </summary>
        protected virtual void ConfigureTestServices(IServiceCollection services)
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Override to indicate if this test requires database
        /// </summary>
        protected virtual bool UsesDatabase => true;

        /// <summary>
        /// Override to indicate if this test requires Redis
        /// </summary>
        protected virtual bool UsesRedis => true;

        /// <summary>
        /// Override to indicate if this test requires authentication
        /// </summary>
        protected virtual bool RequiresAuthentication => true;

        /// <summary>
        /// Authenticates the HTTP client with a test token
        /// </summary>
        protected virtual async Task AuthenticateAsync()
        {
            // Generate a test JWT token
            var token = await GenerateTestTokenAsync();
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Generates a test JWT token for authentication
        /// </summary>
        protected virtual async Task<string> GenerateTestTokenAsync()
        {
            // In a real implementation, this would call the auth endpoint
            // For now, return a placeholder
            await Task.CompletedTask;
            return "test-jwt-token";
        }

        /// <summary>
        /// Creates a scope for resolving services
        /// </summary>
        protected IServiceScope CreateScope()
        {
            return Services.CreateScope();
        }

        /// <summary>
        /// Gets a service from the DI container
        /// </summary>
        protected T GetService<T>() where T : notnull
        {
            using var scope = CreateScope();
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Clears test data between tests
        /// </summary>
        protected async Task ClearTestDataAsync()
        {
            if (DatabaseFixture != null)
            {
                await DatabaseFixture.ClearDataAsync();
            }

            if (RedisFixture != null)
            {
                await RedisFixture.FlushDatabaseAsync();
            }
        }
    }
}