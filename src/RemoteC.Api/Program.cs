using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using RemoteC.Data;
using Serilog;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using RemoteC.Api.Hubs;
using RemoteC.Api.Services;
using RemoteC.Api.Middleware;
using RemoteC.Shared.Models;
using StackExchange.Redis;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RemoteC.Api.Authentication;

namespace RemoteC.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        builder.Host.UseSerilog();

        try
        {
            Log.Information("Starting RemoteC API");

            // Add Azure Key Vault configuration
            if (!builder.Environment.IsDevelopment())
            {
                var keyVaultName = builder.Configuration["KeyVault:VaultName"];
                if (!string.IsNullOrEmpty(keyVaultName) && keyVaultName != "your_keyvault_name")
                {
                    try
                    {
                        var credential = new DefaultAzureCredential();
                        var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), credential);
                        builder.Configuration.AddAzureKeyVault(client, new Azure.Extensions.AspNetCore.Configuration.Secrets.AzureKeyVaultConfigurationOptions());
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to configure Azure Key Vault: {Message}. Continuing without Key Vault.", ex.Message);
                    }
                }
            }
            else
            {
                Log.Information("Running in Development mode - Azure Key Vault is disabled");
            }

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "RemoteC API",
                    Version = "v1",
                    Description = "Enterprise Remote Control Solution API",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "RemoteC Team",
                        Email = "support@remotec.io",
                        Url = new Uri("https://remotec.io")
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Include XML comments
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            // Add Entity Framework
            builder.Services.AddDbContext<RemoteCDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (connectionString?.Contains(".db") == true)
                {
                    options.UseSqlite(connectionString);
                }
                else
                {
                    options.UseSqlServer(connectionString);
                }
            });

            // Add Azure AD B2C Authentication (disabled in development for faster startup)
            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));
                    
                Log.Information("Azure AD B2C authentication enabled for production");
            }
            else
            {
                // Use simple development authentication
                builder.Services.AddAuthentication("Development")
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, 
                        DevelopmentAuthenticationHandler>("Development", options => { });
                        
                Log.Information("Development authentication enabled (Azure AD B2C disabled for faster startup)");
            }

            // Add Authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAuthenticatedUser", policy =>
                    policy.RequireAuthenticatedUser());
                options.AddPolicy("RequireAdminRole", policy =>
                    policy.RequireRole("Admin"));
                options.AddPolicy("RequireOperatorRole", policy =>
                    policy.RequireRole("Admin", "Operator"));
            });

            // Add SignalR
            builder.Services.AddSignalR();

            // Add Redis for caching and session state
            var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnectionString) && redisConnectionString != "localhost:6379")
            {
                builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(redisConnectionString));
                builder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                });
                Log.Information("Redis distributed cache enabled: {ConnectionString}", redisConnectionString);
            }
            else
            {
                // Use memory cache when Redis is not available
                builder.Services.AddMemoryCache();
                builder.Services.AddDistributedMemoryCache();
                Log.Information("Using memory distributed cache (Redis disabled)");
            }

            // Add Hangfire for background jobs (conditionally)
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var hangfireEnabled = builder.Configuration.GetValue<bool>("Hangfire:Enabled", false); // Default to false for development
            
            if (hangfireEnabled && !string.IsNullOrEmpty(connectionString) && !connectionString.Contains(".db") && !connectionString.Contains("LocalDB"))
            {
                try
                {
                    builder.Services.AddHangfire(configuration => configuration
                        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                        .UseSimpleAssemblyNameTypeSerializer()
                        .UseRecommendedSerializerSettings()
                        .UseSqlServerStorage(connectionString));

                    builder.Services.AddHangfireServer();
                    Log.Information("Hangfire enabled with SQL Server storage");
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to configure Hangfire: {Message}. Continuing without Hangfire.", ex.Message);
                }
            }
            else
            {
                Log.Information("Hangfire is disabled (SQLite database, LocalDB, or Hangfire:Enabled=false)");
            }

            // Add AutoMapper (using built-in DI support in AutoMapper 15)
            builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));

            // Add Repositories
            builder.Services.AddScoped<Data.Repositories.IUserRepository, Data.Repositories.UserRepository>();
            builder.Services.AddScoped<Data.Repositories.ISessionRepository, Data.Repositories.SessionRepository>();
            builder.Services.AddScoped<Data.Repositories.IAuditRepository, Data.Repositories.AuditRepository>();
            // Use development repository in development mode
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddScoped<Data.Repositories.IDeviceRepository, Data.Repositories.DeviceRepositoryDev>();
            }
            else
            {
                builder.Services.AddScoped<Data.Repositories.IDeviceRepository, Data.Repositories.DeviceRepository>();
            }

            // Add Application Services
            builder.Services.AddScoped<ISessionService, SessionService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IPinService, PinService>();
            builder.Services.AddScoped<IRemoteControlService, RemoteControlService>();
            builder.Services.AddScoped<ICommandExecutionService, CommandExecutionService>();
            builder.Services.AddScoped<IFileTransferService, FileTransferService>();
            builder.Services.AddScoped<IAuditService, AuditService>();
            builder.Services.AddScoped<IEncryptionService, EncryptionService>();
            builder.Services.AddScoped<IE2EEncryptionService, E2EEncryptionService>();
            builder.Services.AddScoped<IAdaptiveQualityService, AdaptiveQualityService>();
            builder.Services.AddScoped<ISessionMetricsService, SessionMetricsService>();
            builder.Services.AddScoped<IMetricsCollector, MetricsCollector>();
            builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            builder.Services.AddHostedService<QueuedHostedService>();
            
            // Add Remote Control Provider Factory
            builder.Services.AddSingleton<IRemoteControlProviderFactory, RemoteControlProviderFactory>();
            builder.Services.AddScoped<IRemoteControlProvider>(sp => 
                sp.GetRequiredService<IRemoteControlProviderFactory>().CreateProvider());
            
            // Add new services
            builder.Services.AddScoped<IComplianceService, ComplianceService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
            builder.Services.AddScoped<ICacheService, CacheService>();
            builder.Services.AddScoped<IMetricsCollector, MetricsCollector>();
            builder.Services.AddScoped<IEdgeDeploymentService, EdgeDeploymentService>();
            builder.Services.AddScoped<IDockerService, DockerService>();
            builder.Services.AddScoped<IKubernetesService, KubernetesService>();
            builder.Services.AddScoped<IRegistryService, RegistryService>();
            builder.Services.AddScoped<IMetricsService, MetricsService>();
            builder.Services.AddScoped<IIdentityProviderService, IdentityProviderService>();
            builder.Services.AddScoped<ICertificateService, CertificateService>();
            builder.Services.AddScoped<IPolicyEngineService, PolicyEngineService>();
            builder.Services.AddScoped<IClipboardService, ClipboardService>();
            
            // Configure file transfer options
            builder.Services.Configure<FileTransferOptions>(builder.Configuration.GetSection("FileTransfer"));
            
            // Configure E2EE options
            builder.Services.Configure<E2EEncryptionOptions>(builder.Configuration.GetSection("E2EEncryption"));
            
            // Configure compliance options
            builder.Services.Configure<ComplianceOptions>(builder.Configuration.GetSection("Compliance"));
            
            // Configure analytics options
            builder.Services.Configure<AnalyticsOptions>(builder.Configuration.GetSection("Analytics"));
            
            // Configure edge deployment options
            builder.Services.Configure<EdgeDeploymentOptions>(builder.Configuration.GetSection("EdgeDeployment"));
            
            // Configure identity provider options
            builder.Services.Configure<IdentityProviderOptions>(options =>
            {
                var config = builder.Configuration.GetSection("IdentityProvider");
                config.Bind(options);
                
                // Generate RSA signing key if not provided
                if (options.SigningKey == null)
                {
                    var rsa = RSA.Create(2048);
                    options.SigningKey = new RsaSecurityKey(rsa)
                    {
                        KeyId = Guid.NewGuid().ToString()
                    };
                }
            });
            
            // Configure audit options
            builder.Services.Configure<AuditOptions>(options =>
            {
                options.EnableBatching = true;
                options.BatchSize = 100;
                options.BatchIntervalSeconds = 5;
                options.MinimumSeverity = RemoteC.Shared.Models.AuditSeverity.Info;
                options.RetentionDays = 365;
                options.FailureAlertThreshold = 5;
                options.ExcludedActions = new List<string> { "HealthCheck", "GetMetrics" };
            });

            // Add HttpClient for ControlR integration
            builder.Services.AddHttpClient("ControlR", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["RemoteControl:ControlR:ApiUrl"] ?? "https://controlr.api");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Add HttpContextAccessor for audit service
            builder.Services.AddHttpContextAccessor();

            // Add Application Insights
            builder.Services.AddApplicationInsightsTelemetry();

            // Register health check dependencies
            builder.Services.AddSingleton<ExternalServicesHealthCheck>();
            builder.Services.AddSingleton<DiskSpaceHealthCheck>(sp => 
                new DiskSpaceHealthCheck(1024L, sp.GetRequiredService<ILogger<DiskSpaceHealthCheck>>()));

            // Add Health Checks
            var healthChecksBuilder = builder.Services.AddHealthChecks()
                .AddDbContextCheck<RemoteCDbContext>("database", tags: new[] { "db", "sql" })
                .AddCheck<ExternalServicesHealthCheck>("external-services", tags: new[] { "external" })
                .AddCheck<DiskSpaceHealthCheck>("disk-space", tags: new[] { "infrastructure" })
                .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "self" });

            // Only add Redis health check if Redis is enabled
            if (!string.IsNullOrEmpty(redisConnectionString) && redisConnectionString != "localhost:6379")
            {
                builder.Services.AddSingleton<RedisHealthCheck>();
                healthChecksBuilder.AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache", "redis" });
            }

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                    policy.WithOrigins(
                        "http://localhost:3000", 
                        "https://localhost:3000",
                        "http://localhost:17002",
                        "http://10.0.0.91:17001",
                        "http://10.0.0.91:17002"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
                
                // Add a more permissive policy for SignalR in development
                if (builder.Environment.IsDevelopment())
                {
                    options.AddPolicy("SignalRDevelopment", policy =>
                        policy.SetIsOriginAllowed(_ => true)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials());
                }
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            
            // Use appropriate CORS policy based on environment
            if (app.Environment.IsDevelopment())
            {
                app.UseCors("SignalRDevelopment");
            }
            else
            {
                app.UseCors("AllowReactApp");
            }

            // Add audit logging middleware
            app.UseAuditLogging(options =>
            {
                options.CaptureRequestBody = true;
                options.CaptureResponseBody = false;
                options.ExcludedPaths = new[] { "/health", "/metrics", "/swagger", "/hangfire" };
                options.CapturedHeaders = new[] { "X-Correlation-Id", "X-Request-Id", "X-Forwarded-For" };
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            
            // Map SignalR hubs with appropriate CORS policy
            if (app.Environment.IsDevelopment())
            {
                app.MapHub<SessionHub>("/hubs/session").RequireCors("SignalRDevelopment");
                app.MapHub<HostHub>("/hubs/host").RequireCors("SignalRDevelopment");
            }
            else
            {
                app.MapHub<SessionHub>("/hubs/session").RequireCors("AllowReactApp");
                app.MapHub<HostHub>("/hubs/host").RequireCors("AllowReactApp");
            }

            // Add Hangfire Dashboard (conditionally)
            try
            {
                if (app.Services.GetService<Hangfire.IGlobalConfiguration>() != null)
                {
                    app.UseHangfireDashboard("/hangfire", new DashboardOptions
                    {
                        Authorization = new[] { new HangfireAuthorizationFilter() }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Hangfire dashboard not available: {Message}", ex.Message);
            }

            // Health checks
            app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    
                    var response = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(x => new
                        {
                            component = x.Key,
                            status = x.Value.Status.ToString(),
                            description = x.Value.Description,
                            duration = x.Value.Duration.TotalMilliseconds,
                            tags = x.Value.Tags
                        }),
                        totalDuration = report.TotalDuration.TotalMilliseconds,
                        timestamp = DateTime.UtcNow
                    };
                    
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                }
            });
            
            app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("cache")
            });
            
            app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("self")
            });

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // In production, implement proper authorization
        return true;
    }
}