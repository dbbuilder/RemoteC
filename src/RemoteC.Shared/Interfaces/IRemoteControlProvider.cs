using System;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Shared.Models
{
    /// <summary>
    /// Interface for remote control provider implementations
    /// </summary>
    public interface IRemoteControlProvider : IDisposable
    {
        /// <summary>
        /// Provider name
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Provider version
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// Initialize the provider
        /// </summary>
        Task<bool> InitializeAsync();
        
        /// <summary>
        /// Start a new remote control session
        /// </summary>
        Task<RemoteSession> StartSessionAsync(string deviceId, string userId);
        
        /// <summary>
        /// End a remote control session
        /// </summary>
        Task<bool> EndSessionAsync(string sessionId);
        
        /// <summary>
        /// Capture a screen frame
        /// </summary>
        Task<ScreenFrame> CaptureScreenAsync(string sessionId);
        
        /// <summary>
        /// Send an input event to the remote device
        /// </summary>
        Task SendInputAsync(string sessionId, InputEvent inputEvent);
        
        /// <summary>
        /// Get session statistics
        /// </summary>
        Task<SessionStatistics> GetStatisticsAsync(string sessionId);
    }
    
    /// <summary>
    /// Remote control session
    /// </summary>
    public class RemoteSession
    {
        public string Id { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public SessionStatus Status { get; set; }
        public string? ConnectionString { get; set; }
    }
    
    /// <summary>
    /// Screen frame data
    /// </summary>
    public class ScreenFrame
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime Timestamp { get; set; }
        public bool IsKeyFrame { get; set; }
        public int CompressionQuality { get; set; }
    }
    
    /// <summary>
    /// Input event base class
    /// </summary>
    public abstract class InputEvent
    {
        public DateTime Timestamp { get; set; }
        public InputEventType Type { get; set; }
    }
    
    /// <summary>
    /// Mouse input event
    /// </summary>
    public class MouseInputEvent : InputEvent
    {
        public int X { get; set; }
        public int Y { get; set; }
        public MouseButton Button { get; set; }
        public MouseAction Action { get; set; }
        public int WheelDelta { get; set; }
        
        public MouseInputEvent()
        {
            Type = InputEventType.Mouse;
        }
    }
    
    /// <summary>
    /// Keyboard input event
    /// </summary>
    public class KeyboardInputEvent : InputEvent
    {
        public string Key { get; set; } = string.Empty;
        public int KeyCode { get; set; }
        public KeyAction Action { get; set; }
        public bool CtrlPressed { get; set; }
        public bool AltPressed { get; set; }
        public bool ShiftPressed { get; set; }
        
        public KeyboardInputEvent()
        {
            Type = InputEventType.Keyboard;
        }
    }
    
    /// <summary>
    /// Session statistics
    /// </summary>
    public class SessionStatistics
    {
        public double FramesPerSecond { get; set; }
        public double Latency { get; set; }
        public long Bandwidth { get; set; }
        public float PacketLoss { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public int FramesEncoded { get; set; }
        public int FramesDropped { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public TimeSpan Duration { get; set; }
    }
    
    /// <summary>
    /// Input event type
    /// </summary>
    public enum InputEventType
    {
        Mouse,
        Keyboard,
        Touch,
        Gesture
    }
}