using System.Threading.Tasks;

namespace RemoteC.Client.Services
{
    public interface IClipboardService
    {
        Task<string?> GetTextAsync();
        Task SetTextAsync(string text);
        Task<bool> HasTextAsync();
        void StartMonitoring();
        void StopMonitoring();
        event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;
    }

    public class ClipboardChangedEventArgs : EventArgs
    {
        public string? Text { get; set; }
        public DateTime Timestamp { get; set; }
    }
}