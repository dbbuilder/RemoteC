using System;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Client.Services
{
    public interface IRemoteControlService
    {
        event EventHandler<ScreenUpdateEventArgs>? ScreenUpdated;
        event EventHandler<SessionStatusEventArgs>? SessionStatusChanged;
        event EventHandler<PermissionRequestEventArgs>? PermissionRequested;

        Task<ConnectResult> ConnectAsync(string deviceId);
        Task<ConnectResult> ConnectWithPinAsync(string deviceId, string pin);
        Task DisconnectAsync(Guid sessionId);
        
        Task SendMouseEventAsync(Guid sessionId, MouseInputEvent mouseEvent);
        Task SendKeyboardEventAsync(Guid sessionId, KeyboardInputEvent keyboardEvent);
        Task SendClipboardDataAsync(Guid sessionId, string data);
        
        Task RequestControlAsync(Guid sessionId);
        Task ReleaseControlAsync(Guid sessionId);
        
        Task<SessionStatistics> GetSessionStatisticsAsync(Guid sessionId);
        Task SetQualityModeAsync(Guid sessionId, QualityMode mode);
    }

    public class ConnectResult
    {
        public bool Success { get; set; }
        public Guid SessionId { get; set; }
        public string? ErrorMessage { get; set; }
        public SessionInfo? SessionInfo { get; set; }
    }

    public class ScreenUpdateEventArgs : EventArgs
    {
        public Guid SessionId { get; set; }
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public int Width { get; set; }
        public int Height { get; set; }
        public ScreenUpdateType UpdateType { get; set; }
        public ScreenRegion? UpdatedRegion { get; set; }
    }

    public class SessionStatusEventArgs : EventArgs
    {
        public Guid SessionId { get; set; }
        public SessionStatus Status { get; set; }
        public string? Message { get; set; }
    }

    public class PermissionRequestEventArgs : EventArgs
    {
        public Guid SessionId { get; set; }
        public string RequestingUser { get; set; } = string.Empty;
        public PermissionType Permission { get; set; }
        public Action<bool> Respond { get; set; } = null!;
    }

    public enum ScreenUpdateType
    {
        Full,
        Partial,
        Cursor
    }

    public enum QualityMode
    {
        Low,
        Medium,
        High,
        Adaptive
    }

    public enum PermissionType
    {
        Control,
        FileTransfer,
        Clipboard,
        Audio
    }
}