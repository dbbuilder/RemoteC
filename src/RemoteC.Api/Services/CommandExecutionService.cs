using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    /// <summary>
    /// Service for executing commands on remote devices
    /// </summary>
    public class CommandExecutionService : ICommandExecutionService
    {
        private readonly ILogger<CommandExecutionService> _logger;
        private readonly IAuditService _auditService;
        private readonly Dictionary<Guid, List<CommandHistoryDto>> _commandHistory;
        
        // List of potentially dangerous commands that should be blocked
        private readonly HashSet<string> _blockedCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "format", "del /s", "rm -rf", "dd", "mkfs", "fdisk", "diskpart",
            "shutdown", "reboot", "poweroff", "halt", "init 0", "init 6",
            "reg delete", "regedit", "bcdedit", "sfc", "dism"
        };

        public CommandExecutionService(
            ILogger<CommandExecutionService> logger,
            IAuditService auditService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _commandHistory = new Dictionary<Guid, List<CommandHistoryDto>>();
        }

        public async Task<CommandExecutionResult> ExecuteCommandAsync(Guid sessionId, string command, string shell = "powershell")
        {
            try
            {
                _logger.LogInformation("Executing command for session {SessionId} in shell {Shell}", sessionId, shell);
                
                // Check if command is allowed
                var isAllowed = await IsCommandAllowedAsync(command);
                if (!isAllowed)
                {
                    _logger.LogWarning("Blocked dangerous command for session {SessionId}: {Command}", sessionId, command);
                    
                    var blockedResult = new CommandExecutionResult
                    {
                        SessionId = sessionId,
                        Command = command,
                        Shell = shell,
                        Output = "Command blocked: This command has been blocked for security reasons.",
                        ErrorOutput = "BLOCKED: Command contains potentially dangerous operations.",
                        ExitCode = -1,
                        ExecutedAt = DateTime.UtcNow,
                        Duration = TimeSpan.Zero,
                        Success = false
                    };
                    
                    await AddToHistoryAsync(sessionId, blockedResult);
                    return blockedResult;
                }

                // Simulate command execution
                var startTime = DateTime.UtcNow;
                
                // TODO: Implement actual command execution through remote control provider
                // For now, return simulated results
                var result = new CommandExecutionResult
                {
                    SessionId = sessionId,
                    Command = command,
                    Shell = shell,
                    Output = GetSimulatedOutput(command),
                    ErrorOutput = string.Empty,
                    ExitCode = 0,
                    ExecutedAt = startTime,
                    Duration = TimeSpan.FromMilliseconds(Random.Shared.Next(100, 500)),
                    Success = true
                };

                // Add to history
                await AddToHistoryAsync(sessionId, result);
                
                // Audit the command execution
                await _auditService.LogAsync(new AuditEvent
                {
                    Action = "CommandExecuted",
                    ResourceType = "Session",
                    ResourceId = sessionId.ToString(),
                    Details = new Dictionary<string, object>
                    {
                        { "Command", command },
                        { "Shell", shell },
                        { "ExitCode", result.ExitCode },
                        { "Duration", result.Duration.TotalMilliseconds }
                    },
                    Severity = AuditSeverity.Medium,
                    Result = result.Success ? "Success" : "Failed"
                });
                
                _logger.LogInformation("Command executed successfully for session {SessionId}", sessionId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute command for session {SessionId}", sessionId);
                
                return new CommandExecutionResult
                {
                    SessionId = sessionId,
                    Command = command,
                    Shell = shell,
                    Output = string.Empty,
                    ErrorOutput = $"Error: {ex.Message}",
                    ExitCode = -1,
                    ExecutedAt = DateTime.UtcNow,
                    Duration = TimeSpan.Zero,
                    Success = false
                };
            }
        }

        public async Task<IEnumerable<CommandHistoryDto>> GetCommandHistoryAsync(Guid sessionId)
        {
            await Task.CompletedTask;
            
            if (_commandHistory.TryGetValue(sessionId, out var history))
            {
                return history.OrderByDescending(h => h.ExecutedAt).ToList();
            }
            
            return Enumerable.Empty<CommandHistoryDto>();
        }

        public async Task<bool> IsCommandAllowedAsync(string command)
        {
            await Task.CompletedTask;
            
            if (string.IsNullOrWhiteSpace(command))
                return false;
            
            // Check against blocked commands
            foreach (var blocked in _blockedCommands)
            {
                if (command.Contains(blocked, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            
            // Additional security checks
            var dangerousPatterns = new[]
            {
                @"rm\s+-rf\s+/",
                @"format\s+[a-zA-Z]:",
                @"del\s+/[sS]\s+/[qQ]",
                @":(){ :|:& };:", // Fork bomb
                @">\s*/dev/sda"   // Direct disk write
            };
            
            foreach (var pattern in dangerousPatterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(command, pattern))
                {
                    return false;
                }
            }
            
            return true;
        }

        private async Task AddToHistoryAsync(Guid sessionId, CommandExecutionResult result)
        {
            await Task.CompletedTask;
            
            if (!_commandHistory.ContainsKey(sessionId))
            {
                _commandHistory[sessionId] = new List<CommandHistoryDto>();
            }
            
            var historyEntry = new CommandHistoryDto
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Command = result.Command,
                Shell = result.Shell,
                ExecutedAt = result.ExecutedAt,
                Duration = result.Duration,
                Success = result.Success,
                ExitCode = result.ExitCode,
                OutputPreview = GetOutputPreview(result.Output, 100)
            };
            
            _commandHistory[sessionId].Add(historyEntry);
            
            // Keep only last 100 commands per session
            if (_commandHistory[sessionId].Count > 100)
            {
                _commandHistory[sessionId].RemoveAt(0);
            }
        }

        private string GetOutputPreview(string output, int maxLength)
        {
            if (string.IsNullOrEmpty(output))
                return string.Empty;
                
            if (output.Length <= maxLength)
                return output;
                
            return output.Substring(0, maxLength) + "...";
        }

        private string GetSimulatedOutput(string command)
        {
            // Simulate some common command outputs for testing
            var lowerCommand = command.ToLowerInvariant().Trim();
            
            if (lowerCommand == "dir" || lowerCommand == "ls")
            {
                return @"Directory: C:\Users\RemoteUser\Documents

Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
d-----         2024-01-15   9:30 AM                Projects
d-----         2024-01-10   2:15 PM                Downloads
-a----         2024-01-20   4:45 PM          2048 notes.txt
-a----         2024-01-18  11:00 AM         15360 report.docx";
            }
            
            if (lowerCommand == "whoami")
            {
                return @"REMOTE-PC\RemoteUser";
            }
            
            if (lowerCommand == "hostname")
            {
                return "REMOTE-PC";
            }
            
            if (lowerCommand.StartsWith("echo "))
            {
                return command.Substring(5);
            }
            
            if (lowerCommand == "ipconfig" || lowerCommand == "ifconfig")
            {
                return @"Ethernet adapter Ethernet:
   IPv4 Address. . . . . . . . . . . : 192.168.1.100
   Subnet Mask . . . . . . . . . . . : 255.255.255.0
   Default Gateway . . . . . . . . . : 192.168.1.1";
            }
            
            return $"Command '{command}' executed successfully.";
        }
    }

    /// <summary>
    /// Result of command execution
    /// </summary>
    public class CommandExecutionResult
    {
        public Guid SessionId { get; set; }
        public string Command { get; set; } = string.Empty;
        public string Shell { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public string ErrorOutput { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public DateTime ExecutedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
    }
}