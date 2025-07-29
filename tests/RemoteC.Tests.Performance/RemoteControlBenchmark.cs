using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using RemoteC.Core.Interop;
using RemoteC.Shared.Models;

namespace RemoteC.Tests.Performance
{
    /// <summary>
    /// Performance benchmarks for remote control operations
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
    public class RemoteControlBenchmark
    {
        private RustRemoteControlProvider? _rustProvider;
        private string? _sessionId;
        private const int FrameCount = 100;
        private const int InputEventCount = 1000;

        [GlobalSetup]
        public async Task Setup()
        {
            _rustProvider = new RustRemoteControlProvider();
            await _rustProvider.InitializeAsync();
            
            var session = await _rustProvider.StartSessionAsync("test-device", "test-user");
            _sessionId = session.Id;
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            if (_sessionId != null && _rustProvider != null)
            {
                await _rustProvider.EndSessionAsync(_sessionId);
            }
            _rustProvider?.Dispose();
        }

        [Benchmark(Description = "Screen Capture Performance")]
        public async Task BenchmarkScreenCapture()
        {
            for (int i = 0; i < FrameCount; i++)
            {
                var frame = await _rustProvider!.CaptureScreenAsync(_sessionId!);
                if (frame.Data.Length == 0)
                {
                    throw new Exception("Failed to capture frame");
                }
            }
        }

        [Benchmark(Description = "Input Simulation Performance")]
        public async Task BenchmarkInputSimulation()
        {
            for (int i = 0; i < InputEventCount; i++)
            {
                var mouseEvent = new MouseInputEvent
                {
                    X = i % 1920,
                    Y = i % 1080,
                    Action = MouseAction.Move,
                    Timestamp = DateTime.UtcNow
                };
                
                await _rustProvider!.SendInputAsync(_sessionId!, mouseEvent);
            }
        }

        [Benchmark(Description = "Combined Capture and Input")]
        public async Task BenchmarkCombinedOperations()
        {
            var captureTask = Task.Run(async () =>
            {
                for (int i = 0; i < FrameCount; i++)
                {
                    await _rustProvider!.CaptureScreenAsync(_sessionId!);
                }
            });

            var inputTask = Task.Run(async () =>
            {
                for (int i = 0; i < InputEventCount / 10; i++)
                {
                    var mouseEvent = new MouseInputEvent
                    {
                        X = i % 1920,
                        Y = i % 1080,
                        Action = MouseAction.Move,
                        Timestamp = DateTime.UtcNow
                    };
                    await _rustProvider!.SendInputAsync(_sessionId!, mouseEvent);
                }
            });

            await Task.WhenAll(captureTask, inputTask);
        }
    }

    /// <summary>
    /// Latency measurements for remote control operations
    /// </summary>
    public class LatencyMeasurement
    {
        private readonly RustRemoteControlProvider _provider;
        private readonly string _sessionId;
        private readonly Stopwatch _stopwatch = new();

        public LatencyMeasurement()
        {
            _provider = new RustRemoteControlProvider();
            _provider.InitializeAsync().Wait();
            var session = _provider.StartSessionAsync("test-device", "test-user").Result;
            _sessionId = session.Id;
        }

        public async Task<LatencyReport> MeasureLatency()
        {
            var report = new LatencyReport();

            // Measure screen capture latency
            var captureLatencies = new List<double>();
            for (int i = 0; i < 100; i++)
            {
                _stopwatch.Restart();
                await _provider.CaptureScreenAsync(_sessionId);
                _stopwatch.Stop();
                captureLatencies.Add(_stopwatch.Elapsed.TotalMilliseconds);
            }

            report.AverageCaptureLatency = captureLatencies.Average();
            report.MinCaptureLatency = captureLatencies.Min();
            report.MaxCaptureLatency = captureLatencies.Max();
            report.P95CaptureLatency = GetPercentile(captureLatencies, 0.95);

            // Measure input latency
            var inputLatencies = new List<double>();
            for (int i = 0; i < 100; i++)
            {
                var mouseEvent = new MouseInputEvent
                {
                    X = i * 10,
                    Y = i * 10,
                    Action = MouseAction.Move,
                    Timestamp = DateTime.UtcNow
                };

                _stopwatch.Restart();
                await _provider.SendInputAsync(_sessionId, mouseEvent);
                _stopwatch.Stop();
                inputLatencies.Add(_stopwatch.Elapsed.TotalMilliseconds);
            }

            report.AverageInputLatency = inputLatencies.Average();
            report.MinInputLatency = inputLatencies.Min();
            report.MaxInputLatency = inputLatencies.Max();
            report.P95InputLatency = GetPercentile(inputLatencies, 0.95);

            // Get session statistics
            var stats = await _provider.GetStatisticsAsync(_sessionId);
            report.NetworkLatency = stats.Latency;
            report.FramesPerSecond = stats.FramesPerSecond;
            report.PacketLoss = stats.PacketLoss;

            return report;
        }

        private double GetPercentile(List<double> values, double percentile)
        {
            values.Sort();
            int index = (int)Math.Ceiling(percentile * values.Count) - 1;
            return values[Math.Max(0, Math.Min(index, values.Count - 1))];
        }

        public void Dispose()
        {
            _provider.EndSessionAsync(_sessionId).Wait();
            _provider.Dispose();
        }
    }

    /// <summary>
    /// Latency report for performance analysis
    /// </summary>
    public class LatencyReport
    {
        public double AverageCaptureLatency { get; set; }
        public double MinCaptureLatency { get; set; }
        public double MaxCaptureLatency { get; set; }
        public double P95CaptureLatency { get; set; }

        public double AverageInputLatency { get; set; }
        public double MinInputLatency { get; set; }
        public double MaxInputLatency { get; set; }
        public double P95InputLatency { get; set; }

        public double NetworkLatency { get; set; }
        public double FramesPerSecond { get; set; }
        public float PacketLoss { get; set; }

        public void PrintReport()
        {
            Console.WriteLine("=== RemoteC Performance Report ===");
            Console.WriteLine($"Screen Capture Latency:");
            Console.WriteLine($"  Average: {AverageCaptureLatency:F2}ms");
            Console.WriteLine($"  Min: {MinCaptureLatency:F2}ms");
            Console.WriteLine($"  Max: {MaxCaptureLatency:F2}ms");
            Console.WriteLine($"  P95: {P95CaptureLatency:F2}ms");
            Console.WriteLine();
            Console.WriteLine($"Input Simulation Latency:");
            Console.WriteLine($"  Average: {AverageInputLatency:F2}ms");
            Console.WriteLine($"  Min: {MinInputLatency:F2}ms");
            Console.WriteLine($"  Max: {MaxInputLatency:F2}ms");
            Console.WriteLine($"  P95: {P95InputLatency:F2}ms");
            Console.WriteLine();
            Console.WriteLine($"Network Statistics:");
            Console.WriteLine($"  Latency: {NetworkLatency:F2}ms");
            Console.WriteLine($"  FPS: {FramesPerSecond:F1}");
            Console.WriteLine($"  Packet Loss: {PacketLoss:P2}");
            Console.WriteLine();
            
            // Performance comparison
            Console.WriteLine("Performance vs ControlR Baseline:");
            if (AverageCaptureLatency < 100)
            {
                Console.WriteLine("✓ Screen capture meets <100ms target");
            }
            else
            {
                Console.WriteLine("✗ Screen capture exceeds 100ms target");
            }
            
            if (NetworkLatency < 50)
            {
                Console.WriteLine("✓ Network latency meets <50ms Phase 2 target");
            }
            else if (NetworkLatency < 100)
            {
                Console.WriteLine("✓ Network latency meets <100ms Phase 1 target");
            }
            else
            {
                Console.WriteLine("✗ Network latency exceeds targets");
            }
        }
    }

    /// <summary>
    /// Benchmark runner
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Run BenchmarkDotNet benchmarks
            Console.WriteLine("Running performance benchmarks...");
            var summary = BenchmarkRunner.Run<RemoteControlBenchmark>();

            // Run latency measurements
            Console.WriteLine("\nMeasuring latency...");
            using var latencyTest = new LatencyMeasurement();
            var report = await latencyTest.MeasureLatency();
            report.PrintReport();

            Console.WriteLine("\nBenchmark complete!");
        }
    }
}