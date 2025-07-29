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
using StackExchange.Redis;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Caching.Distributed;

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
                if (!string.IsNullOrEmpty(keyVaultName))
                {
                    var credential = new DefaultAzureCredential();
                    var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), credential);
                    builder.Configuration.AddAzureKeyVault(client, new Azure.Extensions.AspNetCore.Configuration.Secrets.AzureKeyVaultConfigurationOptions());
                }
            }

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add Entity Framework
            builder.Services.AddDbContext<RemoteCDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add Azure AD B2C Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));

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
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(redisConnectionString));
                builder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                });
            }

            // Add Hangfire for background jobs
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHangfireServer();

            // Add AutoMapper
            builder.Services.AddAutoMapper(typeof(Program));

            // Add Repositories
            builder.Services.AddScoped<Data.Repositories.IUserRepository, Data.Repositories.UserRepository>();
            builder.Services.AddScoped<Data.Repositories.ISessionRepository, Data.Repositories.SessionRepository>();
            builder.Services.AddScoped<Data.Repositories.IAuditRepository, Data.Repositories.AuditRepository>();

            // Add Application Services
            builder.Services.AddScoped<ISessionService, SessionService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IPinService, PinService>();
            builder.Services.AddScoped<IRemoteControlService, RemoteControlService>();
            builder.Services.AddScoped<ICommandExecutionService, CommandExecutionService>();
            builder.Services.AddScoped<IFileTransferService, FileTransferService>();
            builder.Services.AddScoped<IAuditService, AuditService>();
            builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            builder.Services.AddHostedService<QueuedHostedService>();
            
            // Configure audit options
            builder.Services.Configure<AuditOptions>(options =>
            {
                options.EnableBatching = true;
                options.BatchSize = 100;
                options.BatchIntervalSeconds = 5;
                options.MinimumSeverity = AuditSeverity.Info;
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

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                    policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowReactApp");

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
            app.MapHub<SessionHub>("/hubs/session");

            // Add Hangfire Dashboard
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() }
            });

            // Health checks
            app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

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