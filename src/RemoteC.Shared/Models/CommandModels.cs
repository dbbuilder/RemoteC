namespace RemoteC.Shared.Models;

public class CommandExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string? ErrorOutput { get; set; }
    public int ExitCode { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class CommandHistoryDto
{
    public Guid Id { get; set; }
    public string Command { get; set; } = string.Empty;
    public string Shell { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
}