using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using RemoteC.Client.Services;
using Serilog;

namespace RemoteC.Client.ViewModels
{
    public class ConnectViewModel : ViewModelBase
    {
        private readonly ILogger _logger = Log.ForContext<ConnectViewModel>();
        private string _deviceId = string.Empty;
        private string _pin = string.Empty;
        private bool _usePin;
        private bool _isConnecting;
        private string _errorMessage = string.Empty;

        public ConnectViewModel()
        {
            // Commands
            var canConnect = this.WhenAnyValue(
                x => x.DeviceId,
                x => x.Pin,
                x => x.UsePin,
                x => x.IsConnecting,
                (deviceId, pin, usePin, connecting) => 
                    !connecting && !string.IsNullOrWhiteSpace(deviceId) && 
                    (!usePin || !string.IsNullOrWhiteSpace(pin)));

            ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync, canConnect);
            CancelCommand = ReactiveCommand.Create(() => { });
        }

        public string DeviceId
        {
            get => _deviceId;
            set => this.RaiseAndSetIfChanged(ref _deviceId, value);
        }

        public string Pin
        {
            get => _pin;
            set => this.RaiseAndSetIfChanged(ref _pin, value);
        }

        public bool UsePin
        {
            get => _usePin;
            set => this.RaiseAndSetIfChanged(ref _usePin, value);
        }

        public bool IsConnecting
        {
            get => _isConnecting;
            set => this.RaiseAndSetIfChanged(ref _isConnecting, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        private async Task ConnectAsync()
        {
            try
            {
                IsConnecting = true;
                ErrorMessage = string.Empty;

                var remoteService = App.Services.GetService(typeof(IRemoteControlService)) as IRemoteControlService;
                if (remoteService == null)
                {
                    ErrorMessage = "Remote control service not available";
                    return;
                }

                var result = UsePin 
                    ? await remoteService.ConnectWithPinAsync(DeviceId, Pin)
                    : await remoteService.ConnectAsync(DeviceId);

                if (result.Success)
                {
                    _logger.Information("Successfully connected to device {DeviceId}", DeviceId);
                    // Navigate to session view
                    if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var mainWindow = desktop.MainWindow?.DataContext as MainWindowViewModel;
                        if (mainWindow != null)
                        {
                            mainWindow.CurrentPage = new SessionViewModel 
                            { 
                                SessionId = result.SessionId,
                                DeviceId = DeviceId
                            };
                            mainWindow.RefreshCommand.Execute(Unit.Default).Subscribe();
                        }
                    }
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Failed to connect";
                    _logger.Warning("Failed to connect to device {DeviceId}: {Error}", DeviceId, ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error connecting to device {DeviceId}", DeviceId);
                ErrorMessage = "An error occurred while connecting";
            }
            finally
            {
                IsConnecting = false;
            }
        }
    }
}