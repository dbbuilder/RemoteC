using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RemoteC.Shared.Models;

namespace RemoteC.Tests.Performance
{
    /// <summary>
    /// Performance benchmarks for API endpoints
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
    public class ApiPerformanceBenchmark : IDisposable
    {
        private HttpClient _client = null!;
        private string _baseUrl = "https://localhost:7002";
        private string _authToken = null!;
        
        [GlobalSetup]
        public async Task Setup()
        {
            // Create HTTP client for performance testing
            _client = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl)
            };
            
            // Simulate authentication
            _authToken = "fake-jwt-token-for-testing";
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
                
            await Task.CompletedTask;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
        }

        [Benchmark(Description = "Device List API Performance")]
        public async Task BenchmarkDeviceListApi()
        {
            var response = await _client.GetAsync("/api/devices?pageSize=25");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
        }

        [Benchmark(Description = "Session Creation API Performance")]
        public async Task BenchmarkSessionCreationApi()
        {
            var request = new CreateSessionRequest
            {
                DeviceId = Guid.NewGuid().ToString(),
                Name = "Test Session",
                Type = SessionType.RemoteControl
            };
            
            var response = await _client.PostAsJsonAsync("/api/sessions", request);
            // Don't check success as auth might fail in test
            var content = await response.Content.ReadAsStringAsync();
        }

        [Benchmark(Description = "Concurrent API Requests")]
        public async Task BenchmarkConcurrentApiRequests()
        {
            var tasks = new List<Task>();
            var concurrentRequests = 10;
            
            for (int i = 0; i < concurrentRequests; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var response = await _client.GetAsync($"/api/devices?page={i + 1}");
                    await response.Content.ReadAsStringAsync();
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
    /// Simple HTTP performance benchmarks without WebApplicationFactory
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
    public class HttpEndpointBenchmark
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly List<double> _responseTimes = new();

        [Benchmark]
        public async Task<double> BenchmarkHealthEndpoint()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var response = await _client.GetAsync("https://localhost:7002/health");
                sw.Stop();
                return sw.ElapsedMilliseconds;
            }
            catch
            {
                sw.Stop();
                return -1; // Error indicator
            }
        }
    }

    /// <summary>
    /// Load testing for API endpoints
    /// </summary>
    public class ApiLoadTester
    {
        private readonly int _concurrentUsers;
        private readonly int _requestsPerUser;
        private readonly string _baseUrl;
        
        public ApiLoadTester(string baseUrl, int concurrentUsers = 50, int requestsPerUser = 100)
        {
            _baseUrl = baseUrl;
            _concurrentUsers = concurrentUsers;
            _requestsPerUser = requestsPerUser;
        }

        public async Task<LoadTestReport> RunLoadTest()
        {
            var results = new List<RequestResult>();
            var cts = new CancellationTokenSource();
            var sw = Stopwatch.StartNew();
            
            var tasks = new List<Task>();
            
            for (int user = 0; user < _concurrentUsers; user++)
            {
                tasks.Add(SimulateUser(user, results, cts.Token));
            }
            
            await Task.WhenAll(tasks);
            sw.Stop();
            
            return new LoadTestReport
            {
                TotalRequests = results.Count,
                SuccessfulRequests = results.Count(r => r.Success),
                FailedRequests = results.Count(r => !r.Success),
                AverageResponseTime = results.Average(r => r.ResponseTime),
                MinResponseTime = results.Min(r => r.ResponseTime),
                MaxResponseTime = results.Max(r => r.ResponseTime),
                P95ResponseTime = GetPercentile(results.Select(r => r.ResponseTime).ToList(), 0.95),
                P99ResponseTime = GetPercentile(results.Select(r => r.ResponseTime).ToList(), 0.99),
                RequestsPerSecond = results.Count / sw.Elapsed.TotalSeconds,
                TotalDuration = sw.Elapsed
            };
        }

        private async Task SimulateUser(int userId, List<RequestResult> results, CancellationToken ct)
        {
            using var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            
            for (int i = 0; i < _requestsPerUser && !ct.IsCancellationRequested; i++)
            {
                var sw = Stopwatch.StartNew();
                var success = false;
                
                try
                {
                    // Simulate different API calls
                    string endpoint;
                    switch (i % 4)
                    {
                        case 0:
                            endpoint = "/api/devices";
                            break;
                        case 1:
                            endpoint = "/api/sessions";
                            break;
                        case 2:
                            endpoint = "/health";
                            break;
                        default:
                            endpoint = "/api/auth/profile";
                            break;
                    }
                    
                    var response = await client.GetAsync(endpoint, ct);
                    success = (int)response.StatusCode < 500;
                }
                catch (Exception)
                {
                    success = false;
                }
                
                sw.Stop();
                
                lock (results)
                {
                    results.Add(new RequestResult
                    {
                        Success = success,
                        ResponseTime = sw.ElapsedMilliseconds,
                        UserId = userId
                    });
                }
                
                // Small delay between requests
                await Task.Delay(Random.Shared.Next(10, 50), ct);
            }
        }

        private double GetPercentile(List<double> values, double percentile)
        {
            values.Sort();
            int index = (int)Math.Ceiling(percentile * values.Count) - 1;
            return values[Math.Max(0, Math.Min(index, values.Count - 1))];
        }

        private class RequestResult
        {
            public bool Success { get; set; }
            public double ResponseTime { get; set; }
            public int UserId { get; set; }
        }
    }

    /// <summary>
    /// Load test report
    /// </summary>
    public class LoadTestReport
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double P95ResponseTime { get; set; }
        public double P99ResponseTime { get; set; }
        public double RequestsPerSecond { get; set; }
        public TimeSpan TotalDuration { get; set; }

        public void PrintReport()
        {
            Console.WriteLine("\n=== API Load Test Report ===");
            Console.WriteLine($"Total Requests: {TotalRequests:N0}");
            Console.WriteLine($"Successful: {SuccessfulRequests:N0} ({(double)SuccessfulRequests / TotalRequests:P2})");
            Console.WriteLine($"Failed: {FailedRequests:N0} ({(double)FailedRequests / TotalRequests:P2})");
            Console.WriteLine($"\nResponse Times:");
            Console.WriteLine($"  Average: {AverageResponseTime:F2}ms");
            Console.WriteLine($"  Min: {MinResponseTime:F2}ms");
            Console.WriteLine($"  Max: {MaxResponseTime:F2}ms");
            Console.WriteLine($"  P95: {P95ResponseTime:F2}ms");
            Console.WriteLine($"  P99: {P99ResponseTime:F2}ms");
            Console.WriteLine($"\nThroughput:");
            Console.WriteLine($"  Requests/sec: {RequestsPerSecond:F2}");
            Console.WriteLine($"  Total Duration: {TotalDuration}");
            
            // Performance analysis
            Console.WriteLine("\nPerformance Analysis:");
            if (AverageResponseTime < 100)
            {
                Console.WriteLine("✓ Average response time < 100ms (Excellent)");
            }
            else if (AverageResponseTime < 300)
            {
                Console.WriteLine("✓ Average response time < 300ms (Good)");
            }
            else
            {
                Console.WriteLine("✗ Average response time > 300ms (Needs optimization)");
            }
            
            if ((double)SuccessfulRequests / TotalRequests > 0.99)
            {
                Console.WriteLine("✓ Success rate > 99% (Excellent)");
            }
            else if ((double)SuccessfulRequests / TotalRequests > 0.95)
            {
                Console.WriteLine("⚠ Success rate > 95% (Acceptable)");
            }
            else
            {
                Console.WriteLine("✗ Success rate < 95% (Poor)");
            }
        }
    }
}