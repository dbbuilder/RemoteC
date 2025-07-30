using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RemoteC.Data;
using Testcontainers.MsSql;
using Xunit;

namespace RemoteC.Tests.Integration.TestFixtures
{
    /// <summary>
    /// Shared database fixture for integration tests using TestContainers
    /// </summary>
    public class DatabaseFixture : IAsyncLifetime
    {
        private readonly MsSqlContainer _msSqlContainer;
        public string ConnectionString { get; private set; } = string.Empty;
        public RemoteCDbContext DbContext { get; private set; } = null!;
        private IServiceScope? _scope;

        public DatabaseFixture()
        {
            _msSqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Test@123Strong!")
                .WithName($"remotec-test-db-{Guid.NewGuid()}")
                .WithCleanUp(true)
                .Build();
        }

        public async Task InitializeAsync()
        {
            // Start the SQL Server container
            await _msSqlContainer.StartAsync();
            
            ConnectionString = _msSqlContainer.GetConnectionString();
            
            // Create a service provider for dependency injection
            var services = new ServiceCollection();
            services.AddDbContext<RemoteCDbContext>(options =>
                options.UseSqlServer(ConnectionString));
            
            var serviceProvider = services.BuildServiceProvider();
            _scope = serviceProvider.CreateScope();
            
            // Create the database context
            DbContext = _scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
            
            // Ensure database is created and migrations are applied
            await DbContext.Database.EnsureCreatedAsync();
            
            // Seed test data if needed
            await SeedTestDataAsync();
        }

        public async Task DisposeAsync()
        {
            _scope?.Dispose();
            await _msSqlContainer.DisposeAsync();
        }

        private async Task SeedTestDataAsync()
        {
            // Add test users
            var testUser = new RemoteC.Data.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            DbContext.Users.Add(testUser);
            
            // Add test organization
            var testOrg = new RemoteC.Data.Entities.Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Organization",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            DbContext.Organizations.Add(testOrg);
            
            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a new DbContext instance for isolated test execution
        /// </summary>
        public RemoteCDbContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<RemoteCDbContext>();
            optionsBuilder.UseSqlServer(ConnectionString);
            return new RemoteCDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Clears all data from the database for test isolation
        /// </summary>
        public async Task ClearDataAsync()
        {
            // Clear data in reverse order of foreign key dependencies
            DbContext.AuditLogs.RemoveRange(DbContext.AuditLogs);
            DbContext.SessionRecordings.RemoveRange(DbContext.SessionRecordings);
            DbContext.FileTransfers.RemoveRange(DbContext.FileTransfers);
            DbContext.Sessions.RemoveRange(DbContext.Sessions);
            DbContext.Devices.RemoveRange(DbContext.Devices);
            DbContext.Users.RemoveRange(DbContext.Users);
            DbContext.Organizations.RemoveRange(DbContext.Organizations);
            
            await DbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Collection definition for tests that share the database fixture
    /// </summary>
    [CollectionDefinition("Database")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}