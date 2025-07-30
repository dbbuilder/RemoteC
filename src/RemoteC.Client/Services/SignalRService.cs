using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace RemoteC.Client.Services
{
    public class SignalRService : ISignalRService, IDisposable
    {
        private readonly ILogger _logger = Log.ForContext<SignalRService>();
        private readonly IConfiguration _configuration;
        private HubConnection? _hubConnection;

        public event EventHandler<ConnectionStateEventArgs>? ConnectionStateChanged;

        public SignalRService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public async Task<bool> ConnectAsync()
        {
            try
            {
                var apiUrl = _configuration["RemoteC:ApiUrl"];
                var hubPath = _configuration["RemoteC:SignalRHub"];
                var hubUrl = $"{apiUrl}{hubPath}";

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.Closed += async (error) =>
                {
                    _logger.Warning("SignalR connection closed: {Error}", error?.Message);
                    ConnectionStateChanged?.Invoke(this, new ConnectionStateEventArgs { IsConnected = false, Error = error?.Message });
                    await Task.CompletedTask;
                };

                _hubConnection.Reconnecting += async (error) =>
                {
                    _logger.Information("SignalR reconnecting: {Error}", error?.Message);
                    await Task.CompletedTask;
                };

                _hubConnection.Reconnected += async (connectionId) =>
                {
                    _logger.Information("SignalR reconnected: {ConnectionId}", connectionId);
                    ConnectionStateChanged?.Invoke(this, new ConnectionStateEventArgs { IsConnected = true });
                    await Task.CompletedTask;
                };

                await _hubConnection.StartAsync();
                _logger.Information("SignalR connected successfully");
                ConnectionStateChanged?.Invoke(this, new ConnectionStateEventArgs { IsConnected = true });
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to connect to SignalR hub");
                ConnectionStateChanged?.Invoke(this, new ConnectionStateEventArgs { IsConnected = false, Error = ex.Message });
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                ConnectionStateChanged?.Invoke(this, new ConnectionStateEventArgs { IsConnected = false });
            }
        }

        public async Task SendAsync(string method, params object[] args)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync(method, args);
            }
            else
            {
                throw new InvalidOperationException("Not connected to SignalR hub");
            }
        }

        public void On<T>(string method, Action<T> handler)
        {
            _hubConnection?.On(method, handler);
        }

        public void Off(string method)
        {
            _hubConnection?.Remove(method);
        }

        public void Dispose()
        {
            // Use async disposal pattern to properly handle ValueTask
            if (_hubConnection != null)
            {
                var disposeTask = _hubConnection.DisposeAsync();
                if (disposeTask.IsCompletedSuccessfully)
                {
                    // Already completed, no need to wait
                    disposeTask.GetAwaiter().GetResult();
                }
                else
                {
                    // Convert to Task and wait
                    disposeTask.AsTask().GetAwaiter().GetResult();
                }
            }
        }
    }
}