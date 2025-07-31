namespace RemoteC.Shared.Models;

/// <summary>
/// Screen data sent from host to viewers
/// </summary>
public class ScreenData
{
    /// <summary>
    /// The encoded image data (Base64 or byte array)
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();
    
    /// <summary>
    /// The image format (e.g., "jpeg", "png", "webp")
    /// </summary>
    public string Format { get; set; } = "jpeg";
    
    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// Timestamp when the screen was captured
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Monitor index for multi-monitor setups
    /// </summary>
    public int MonitorIndex { get; set; }
    
    /// <summary>
    /// Compression quality used (0-100)
    /// </summary>
    public int Quality { get; set; }
}

/// <summary>
/// Result of command execution
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Unique command ID
    /// </summary>
    public Guid CommandId { get; set; }
    
    /// <summary>
    /// Whether the command executed successfully
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Command output (stdout)
    /// </summary>
    public string? Output { get; set; }
    
    /// <summary>
    /// Error output (stderr)
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Exit code of the command
    /// </summary>
    public int ExitCode { get; set; }
    
    /// <summary>
    /// Execution time in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}

/// <summary>
/// Host health status report
/// </summary>
public class HostHealthStatus
{
    /// <summary>
    /// Overall health status
    /// </summary>
    public bool IsHealthy { get; set; }
    
    /// <summary>
    /// CPU usage percentage (0-100)
    /// </summary>
    public double CpuUsage { get; set; }
    
    /// <summary>
    /// Memory usage percentage (0-100)
    /// </summary>
    public double MemoryUsage { get; set; }
    
    /// <summary>
    /// Disk usage percentage (0-100)
    /// </summary>
    public double DiskUsage { get; set; }
    
    /// <summary>
    /// Network latency in milliseconds
    /// </summary>
    public double NetworkLatencyMs { get; set; }
    
    /// <summary>
    /// Number of active sessions
    /// </summary>
    public int ActiveSessions { get; set; }
    
    /// <summary>
    /// Host uptime
    /// </summary>
    public TimeSpan Uptime { get; set; }
    
    /// <summary>
    /// Timestamp of the health report
    /// </summary>
    public DateTime LastReportTime { get; set; }
    
    /// <summary>
    /// List of any active alerts or issues
    /// </summary>
    public List<string> Alerts { get; set; } = new();
}