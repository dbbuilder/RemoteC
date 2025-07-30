using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;

namespace RemoteC.Tests.Performance
{
    /// <summary>
    /// Performance benchmarks for database operations
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
    public class DatabasePerformanceBenchmark : IDisposable
    {
        private readonly List<Guid> _testUserIds = new();
        private readonly List<Guid> _testDeviceIds = new();
        private readonly List<object> _simulatedData = new();
        
        [GlobalSetup]
        public async Task Setup()
        {
            // Create test data in memory
            await SeedTestData();
        }

        private async Task SeedTestData()
        {
            // Create test organization
            var orgId = Guid.NewGuid();
            
            // Create test users
            for (int i = 0; i < 100; i++)
            {
                var userId = Guid.NewGuid();
                _testUserIds.Add(userId);
                _simulatedData.Add(new
                {
                    Id = userId,
                    Email = $"user{i}@test.com",
                    FirstName = $"Test{i}",
                    LastName = "User",
                    IsActive = true,
                    OrganizationId = orgId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            
            // Create test devices
            for (int i = 0; i < 50; i++)
            {
                var deviceId = Guid.NewGuid();
                _testDeviceIds.Add(deviceId);
                _simulatedData.Add(new
                {
                    Id = deviceId,
                    Name = $"Device{i}",
                    MacAddress = $"00:11:22:33:44:{i:X2}",
                    IsOnline = i % 2 == 0,
                    CreatedBy = _testUserIds[i % _testUserIds.Count],
                    OrganizationId = orgId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            
            await Task.CompletedTask;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _simulatedData.Clear();
            _testUserIds.Clear();
            _testDeviceIds.Clear();
        }

        [Benchmark(Description = "User Query Performance Simulation")]
        public async Task BenchmarkUserQuery()
        {
            // Simulate database query delay
            await Task.Delay(Random.Shared.Next(5, 15));
            
            // Simulate filtering and sorting
            var users = _simulatedData
                .Where(u => u.GetType().GetProperty("Email") != null)
                .Take(25)
                .ToList();
        }

        [Benchmark(Description = "Device Query with Joins Simulation")]
        public async Task BenchmarkDeviceQueryWithJoins()
        {
            // Simulate join operation delay
            await Task.Delay(Random.Shared.Next(10, 25));
            
            // Simulate filtering and sorting
            var devices = _simulatedData
                .Where(d => d.GetType().GetProperty("MacAddress") != null)
                .Take(10)
                .ToList();
        }

        [Benchmark(Description = "Complex Session Query Simulation")]
        public async Task BenchmarkComplexSessionQuery()
        {
            // Simulate complex query with multiple joins
            await Task.Delay(Random.Shared.Next(20, 40));
            
            // Simulate session data
            var session = new
            {
                Id = Guid.NewGuid(),
                Name = "Test Session",
                DeviceId = _testDeviceIds.FirstOrDefault(),
                CreatedBy = _testUserIds.FirstOrDefault(),
                OrganizationId = Guid.NewGuid(),
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            
            _simulatedData.Add(session);
        }

        [Benchmark(Description = "Bulk Insert Performance Simulation")]
        public async Task BenchmarkBulkInsert()
        {
            var auditLogs = new List<object>();
            
            for (int i = 0; i < 1000; i++)
            {
                auditLogs.Add(new
                {
                    Id = Guid.NewGuid(),
                    Action = "TestAction",
                    ResourceType = "TestResource",
                    UserId = _testUserIds[i % _testUserIds.Count],
                    Timestamp = DateTime.UtcNow,
                    Success = true
                });
            }
            
            // Simulate bulk insert delay
            await Task.Delay(Random.Shared.Next(50, 100));
            
            // Add to simulated data
            _simulatedData.AddRange(auditLogs);
        }

        [Benchmark(Description = "Concurrent Database Operations Simulation")]
        public async Task BenchmarkConcurrentOperations()
        {
            var tasks = new List<Task>();
            
            // Simulate concurrent reads
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    // Simulate query delay
                    await Task.Delay(Random.Shared.Next(5, 20));
                    
                    // Simulate data access
                    var users = _simulatedData
                        .Where(u => u.GetType().GetProperty("Email") != null)
                        .ToList();
                }));
            }
            
            await Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            Cleanup();
        }
    }

    /// <summary>
    /// Database query analyzer
    /// </summary>
    public class DatabaseQueryAnalyzer
    {
        private readonly string _connectionString;
        
        public DatabaseQueryAnalyzer(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<QueryPerformanceReport> AnalyzeStoredProcedurePerformance()
        {
            var report = new QueryPerformanceReport();
            
            // Test common stored procedures
            var procedures = new[]
            {
                ("sp_User_GetByEmail", "@Email", "test@example.com"),
                ("sp_Device_GetByUserId", "@UserId", Guid.NewGuid().ToString()),
                ("sp_Session_GetActive", "@OrganizationId", Guid.NewGuid().ToString()),
                ("sp_AuditLog_GetRecent", "@Days", "7")
            };

            foreach (var (procName, paramName, paramValue) in procedures)
            {
                try
                {
                    var timing = await MeasureStoredProcedure(procName, paramName, paramValue);
                    report.ProcedureTimings[procName] = timing;
                }
                catch (Exception ex)
                {
                    report.Errors.Add($"{procName}: {ex.Message}");
                }
            }
            
            return report;
        }

        private async Task<double> MeasureStoredProcedure(string procName, string paramName, string paramValue)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand(procName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue(paramName, paramValue);
            
            var sw = Stopwatch.StartNew();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // Read all results
            }
            sw.Stop();
            
            return sw.ElapsedMilliseconds;
        }
    }

    /// <summary>
    /// Query performance report
    /// </summary>
    public class QueryPerformanceReport
    {
        public Dictionary<string, double> ProcedureTimings { get; } = new();
        public List<string> Errors { get; } = new();
        public List<string> Recommendations { get; } = new();

        public void AnalyzeAndPrintReport()
        {
            Console.WriteLine("\n=== Database Performance Report ===");
            
            if (ProcedureTimings.Any())
            {
                Console.WriteLine("\nStored Procedure Performance:");
                foreach (var (proc, timing) in ProcedureTimings.OrderBy(p => p.Value))
                {
                    Console.WriteLine($"  {proc}: {timing:F2}ms");
                    
                    if (timing < 10)
                    {
                        Console.WriteLine($"    ✓ Excellent performance");
                    }
                    else if (timing < 50)
                    {
                        Console.WriteLine($"    ✓ Good performance");
                    }
                    else if (timing < 100)
                    {
                        Console.WriteLine($"    ⚠ Acceptable performance");
                    }
                    else
                    {
                        Console.WriteLine($"    ✗ Poor performance - needs optimization");
                        Recommendations.Add($"Optimize {proc} - consider adding indexes or rewriting query");
                    }
                }
            }
            
            if (Errors.Any())
            {
                Console.WriteLine("\nErrors:");
                foreach (var error in Errors)
                {
                    Console.WriteLine($"  ✗ {error}");
                }
            }
            
            // General recommendations
            Console.WriteLine("\nGeneral Recommendations:");
            Console.WriteLine("  • Ensure all foreign keys have indexes");
            Console.WriteLine("  • Use covering indexes for frequently accessed columns");
            Console.WriteLine("  • Consider partitioning large tables (AuditLogs, SessionLogs)");
            Console.WriteLine("  • Enable query store for performance monitoring");
            Console.WriteLine("  • Review execution plans for queries > 50ms");
            
            if (Recommendations.Any())
            {
                Console.WriteLine("\nSpecific Recommendations:");
                foreach (var rec in Recommendations)
                {
                    Console.WriteLine($"  • {rec}");
                }
            }
        }
    }
}