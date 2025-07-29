using System;
using System.Threading.Tasks;

namespace RemoteC.Client.Services
{
    public interface ISignalRService
    {
        event EventHandler<ConnectionStateEventArgs>? ConnectionStateChanged;
        
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }
        
        Task SendAsync(string method, params object[] args);
        void On<T>(string method, Action<T> handler);
        void Off(string method);
    }

    public class ConnectionStateEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string? Error { get; set; }
    }
}