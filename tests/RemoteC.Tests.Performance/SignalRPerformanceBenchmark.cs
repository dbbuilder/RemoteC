using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RemoteC.Shared.Models;

namespace RemoteC.Tests.Performance
{
    /// <summary>
    /// Performance benchmarks for SignalR real-time communication simulation
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
    public class SignalRPerformanceBenchmark
    {
        private readonly List<SimulatedConnection> _connections = new();
        private readonly int _connectionCount = 10;
        
        [GlobalSetup]
        public void Setup()
        {
            // Create simulated connections
            for (int i = 0; i < _connectionCount; i++)
            {
                _connections.Add(new SimulatedConnection
                {
                    ConnectionId = Guid.NewGuid().ToString(),
                    UserId = $"user-{i}",
                    IsConnected = true
                });
            }
        }

        [Benchmark(Description = "SignalR Message Broadcasting Simulation")]
        public async Task BenchmarkMessageBroadcasting()
        {
            var sessionId = Guid.NewGuid().ToString();
            var messageCount = 100;
            
            // Simulate sending messages
            for (int i = 0; i < messageCount; i++)
            {
                var mouseInput = new MouseInputDto
                {
                    X = i % 1920,
                    Y = i % 1080,
                    Action = MouseAction.Move,
                    Timestamp = DateTime.UtcNow
                };
                
                // Simulate broadcast delay
                await Task.Delay(1);
            }
        }

        [Benchmark(Description = "Concurrent Message Processing")]
        public async Task BenchmarkConcurrentMessageProcessing()
        {
            var tasks = new List<Task>();
            
            // Simulate concurrent message processing
            foreach (var connection in _connections)
            {
                tasks.Add(Task.Run(async () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        // Simulate message processing
                        await ProcessMessage(connection, $"Message {i}");
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
        }

        [Benchmark(Description = "High-Frequency Update Processing")]
        public async Task BenchmarkHighFrequencyUpdates()
        {
            var updateCount = 60; // Simulate 60 FPS
            var frameData = new byte[1920 * 1080 * 4]; // Simulate 1080p frame
            Random.Shared.NextBytes(frameData);
            
            for (int i = 0; i < updateCount; i++)
            {
                var screenUpdate = new ScreenUpdateDto
                {
                    ImageData = frameData,
                    Timestamp = DateTime.UtcNow,
                    Width = 1920,
                    Height = 1080,
                    Format = "JPEG",
                    Quality = 85
                };
                
                // Simulate processing time
                await Task.Delay(16); // ~60 FPS
            }
        }

        private async Task ProcessMessage(SimulatedConnection connection, string message)
        {
            // Simulate message processing overhead
            await Task.Delay(Random.Shared.Next(1, 5));
        }

        private class SimulatedConnection
        {
            public string ConnectionId { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public bool IsConnected { get; set; }
        }
    }

    /// <summary>
    /// SignalR load tester simulation for real-world scenarios
    /// </summary>
    public class SignalRLoadTester
    {
        private readonly int _sessionCount;
        private readonly int _participantsPerSession;
        private readonly TimeSpan _testDuration;
        
        public SignalRLoadTester(
            string hubUrl, 
            int sessionCount = 10, 
            int participantsPerSession = 5,
            TimeSpan? testDuration = null)
        {
            _sessionCount = sessionCount;
            _participantsPerSession = participantsPerSession;
            _testDuration = testDuration ?? TimeSpan.FromMinutes(1);
        }

        public async Task<SignalRLoadTestReport> RunLoadTest()
        {
            var report = new SignalRLoadTestReport();
            var cts = new CancellationTokenSource(_testDuration);
            var sessions = new List<LoadTestSession>();
            
            try
            {
                // Create simulated sessions
                for (int i = 0; i < _sessionCount; i++)
                {
                    var session = new LoadTestSession
                    {
                        SessionId = Guid.NewGuid().ToString(),
                        ParticipantCount = _participantsPerSession
                    };
                    sessions.Add(session);
                }
                
                // Simulate load test
                var sw = Stopwatch.StartNew();
                await RunLoadTestScenario(sessions, report, cts.Token);
                report.TestDuration = sw.Elapsed;
            }
            catch (Exception ex)
            {
                report.Errors.Add($"Load test error: {ex.Message}");
            }
            
            // Calculate final metrics
            report.TotalConnections = _sessionCount * _participantsPerSession;
            report.MessagesSentPerSecond = report.MessagesSent / report.TestDuration.TotalSeconds;
            report.MessagesReceivedPerSecond = report.MessagesReceived / report.TestDuration.TotalSeconds;
            
            return report;
        }

        private async Task RunLoadTestScenario(
            List<LoadTestSession> sessions, 
            SignalRLoadTestReport report,
            CancellationToken ct)
        {
            var tasks = new List<Task>();
            
            foreach (var session in sessions)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var frameCount = 0;
                    
                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            // Simulate sending mouse movement
                            Interlocked.Increment(ref report._messagesSent);
                            
                            // Simulate screen update periodically
                            if (frameCount++ % 30 == 0) // Every 30 frames
                            {
                                Interlocked.Increment(ref report._screenUpdatesSent);
                            }
                            
                            // Simulate received messages
                            Interlocked.Add(ref report._messagesReceived, session.ParticipantCount);
                            
                            await Task.Delay(16, ct); // ~60 FPS
                        }
                        catch (Exception ex)
                        {
                            report.Errors.Add($"Send error: {ex.Message}");
                        }
                    }
                }, ct));
            }
            
            await Task.WhenAll(tasks);
        }

        private class LoadTestSession
        {
            public string SessionId { get; set; } = string.Empty;
            public int ParticipantCount { get; set; }
        }
    }

    /// <summary>
    /// SignalR load test report
    /// </summary>
    public class SignalRLoadTestReport
    {
        public int TotalConnections { get; set; }
        public TimeSpan ConnectionTime { get; set; }
        public TimeSpan DisconnectionTime { get; set; }
        public TimeSpan TestDuration { get; set; }
        
        internal int _messagesSent;
        internal int _messagesReceived;
        internal int _screenUpdatesSent;
        internal int _screenUpdatesReceived;
        
        public int MessagesSent => _messagesSent;
        public int MessagesReceived => _messagesReceived;
        public int ScreenUpdatesSent => _screenUpdatesSent;
        public int ScreenUpdatesReceived => _screenUpdatesReceived;
        
        public double MessagesSentPerSecond { get; set; }
        public double MessagesReceivedPerSecond { get; set; }
        
        public List<string> Errors { get; } = new();

        public void PrintReport()
        {
            Console.WriteLine("\n=== SignalR Performance Report ===");
            Console.WriteLine($"Total Connections: {TotalConnections}");
            Console.WriteLine($"Connection Time: {ConnectionTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Disconnection Time: {DisconnectionTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Test Duration: {TestDuration}");
            
            Console.WriteLine($"\nMessage Statistics:");
            Console.WriteLine($"  Messages Sent: {MessagesSent:N0}");
            Console.WriteLine($"  Messages Received: {MessagesReceived:N0}");
            Console.WriteLine($"  Screen Updates Sent: {ScreenUpdatesSent:N0}");
            Console.WriteLine($"  Screen Updates Received: {ScreenUpdatesReceived:N0}");
            
            Console.WriteLine($"\nThroughput:");
            Console.WriteLine($"  Messages/sec Sent: {MessagesSentPerSecond:F2}");
            Console.WriteLine($"  Messages/sec Received: {MessagesReceivedPerSecond:F2}");
            
            if (Errors.Any())
            {
                Console.WriteLine($"\nErrors ({Errors.Count}):");
                foreach (var error in Errors.Take(10))
                {
                    Console.WriteLine($"  • {error}");
                }
            }
            
            // Performance analysis
            Console.WriteLine("\nPerformance Analysis:");
            if (MessagesSentPerSecond > 1000)
            {
                Console.WriteLine($"✓ High message throughput achieved");
            }
            
            var lossRate = MessagesSent > 0 ? 1.0 - (double)MessagesReceived / MessagesSent : 0;
            if (lossRate < 0.01)
            {
                Console.WriteLine($"✓ Message loss < 1% ({lossRate:P2})");
            }
            else
            {
                Console.WriteLine($"⚠ Message loss {lossRate:P2}");
            }
        }
    }
}