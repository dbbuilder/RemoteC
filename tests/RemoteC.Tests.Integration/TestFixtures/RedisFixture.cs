using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace RemoteC.Tests.Integration.TestFixtures
{
    /// <summary>
    /// Shared Redis fixture for integration tests using TestContainers
    /// </summary>
    public class RedisFixture : IAsyncLifetime
    {
        private readonly RedisContainer _redisContainer;
        public string ConnectionString { get; private set; } = string.Empty;
        public IConnectionMultiplexer? ConnectionMultiplexer { get; private set; }
        public IDatabase? Database { get; private set; }

        public RedisFixture()
        {
            _redisContainer = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .WithName($"remotec-test-redis-{Guid.NewGuid()}")
                .WithCleanUp(true)
                .Build();
        }

        public async Task InitializeAsync()
        {
            // Start the Redis container
            await _redisContainer.StartAsync();
            
            ConnectionString = _redisContainer.GetConnectionString();
            
            // Create connection multiplexer
            ConnectionMultiplexer = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(ConnectionString);
            Database = ConnectionMultiplexer.GetDatabase();
            
            // Verify connection
            var ping = await Database.PingAsync();
            if (ping.TotalMilliseconds > 1000)
            {
                throw new InvalidOperationException($"Redis ping took too long: {ping.TotalMilliseconds}ms");
            }
        }

        public async Task DisposeAsync()
        {
            if (ConnectionMultiplexer != null)
            {
                await ConnectionMultiplexer.CloseAsync();
                ConnectionMultiplexer.Dispose();
            }
            
            await _redisContainer.DisposeAsync();
        }

        /// <summary>
        /// Clears all data from Redis for test isolation
        /// </summary>
        public async Task FlushDatabaseAsync()
        {
            if (Database != null)
            {
                await Database.ExecuteAsync("FLUSHDB");
            }
        }

        /// <summary>
        /// Gets a new database connection for isolated testing
        /// </summary>
        public IDatabase GetDatabase(int db = 0)
        {
            return ConnectionMultiplexer?.GetDatabase(db) ?? throw new InvalidOperationException("Redis not initialized");
        }
    }

    /// <summary>
    /// Collection definition for tests that share the Redis fixture
    /// </summary>
    [CollectionDefinition("Redis")]
    public class RedisCollection : ICollectionFixture<RedisFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}