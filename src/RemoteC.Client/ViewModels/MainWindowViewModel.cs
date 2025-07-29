using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using RemoteC.Client.Services;
using RemoteC.Shared.Models;
using Serilog;

namespace RemoteC.Client.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ILogger _logger = Log.ForContext<MainWindowViewModel>();
        private readonly IAuthenticationService _authService;
        private readonly ISessionService _sessionService;
        private readonly IRemoteControlService _remoteControlService;
        
        private ViewModelBase _currentPage = null!;
        private bool _isAuthenticated;
        private string _userName = string.Empty;
        private ObservableCollection<SessionViewModel> _activeSessions = new();

        public MainWindowViewModel(
            IAuthenticationService authService,
            ISessionService sessionService,
            IRemoteControlService remoteControlService)
        {
            _authService = authService;
            _sessionService = sessionService;
            _remoteControlService = remoteControlService;

            // Commands
            ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync);
            DisconnectCommand = ReactiveCommand.CreateFromTask<SessionViewModel>(DisconnectAsync);
            SettingsCommand = ReactiveCommand.Create(ShowSettings);
            LogoutCommand = ReactiveCommand.CreateFromTask(LogoutAsync);
            RefreshCommand = ReactiveCommand.CreateFromTask(RefreshSessionsAsync);

            // Initialize
            CurrentPage = new ConnectViewModel();
            _ = InitializeAsync();
        }

        public ViewModelBase CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set => this.RaiseAndSetIfChanged(ref _isAuthenticated, value);
        }

        public string UserName
        {
            get => _userName;
            set => this.RaiseAndSetIfChanged(ref _userName, value);
        }

        public ObservableCollection<SessionViewModel> ActiveSessions
        {
            get => _activeSessions;
            set => this.RaiseAndSetIfChanged(ref _activeSessions, value);
        }

        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
        public ReactiveCommand<SessionViewModel, Unit> DisconnectCommand { get; }
        public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

        private async Task InitializeAsync()
        {
            try
            {
                // Check if already authenticated
                IsAuthenticated = await _authService.IsAuthenticatedAsync();
                if (IsAuthenticated)
                {
                    var user = await _authService.GetCurrentUserAsync();
                    UserName = user?.FullName ?? user?.Email ?? "User";
                    await RefreshSessionsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize application");
            }
        }

        private async Task ConnectAsync()
        {
            try
            {
                if (!IsAuthenticated)
                {
                    var result = await _authService.LoginAsync();
                    if (!result.Success)
                    {
                        _logger.Warning("Authentication failed: {Message}", result.ErrorMessage);
                        return;
                    }

                    IsAuthenticated = true;
                    var user = await _authService.GetCurrentUserAsync();
                    UserName = user?.FullName ?? user?.Email ?? "User";
                }

                // Show connect dialog
                CurrentPage = new ConnectViewModel();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to connect");
            }
        }

        private async Task DisconnectAsync(SessionViewModel session)
        {
            try
            {
                await _remoteControlService.DisconnectAsync(session.SessionId);
                ActiveSessions.Remove(session);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to disconnect session {SessionId}", session.SessionId);
            }
        }

        private void ShowSettings()
        {
            CurrentPage = new SettingsViewModel();
        }

        private async Task LogoutAsync()
        {
            try
            {
                // Disconnect all sessions
                foreach (var session in ActiveSessions)
                {
                    await _remoteControlService.DisconnectAsync(session.SessionId);
                }
                ActiveSessions.Clear();

                // Logout
                await _authService.LogoutAsync();
                IsAuthenticated = false;
                UserName = string.Empty;
                CurrentPage = new ConnectViewModel();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to logout");
            }
        }

        private async Task RefreshSessionsAsync()
        {
            try
            {
                var sessions = await _sessionService.GetActiveSessionsAsync();
                ActiveSessions.Clear();
                foreach (var session in sessions)
                {
                    ActiveSessions.Add(new SessionViewModel 
                    { 
                        SessionId = session.Id,
                        DeviceId = session.DeviceId,
                        DeviceName = session.DeviceName,
                        ConnectedAt = session.CreatedAt,
                        Status = session.Status.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to refresh sessions");
            }
        }
    }
}