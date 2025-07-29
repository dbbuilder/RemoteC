using System;
using ReactiveUI;

namespace RemoteC.Client.ViewModels
{
    public class SessionViewModel : ViewModelBase
    {
        private Guid _sessionId;
        private string _deviceId = string.Empty;
        private string _deviceName = string.Empty;
        private DateTime _connectedAt;
        private string _status = string.Empty;
        private bool _hasControl;
        private int _latency;
        private int _fps;
        private string _quality = string.Empty;

        public Guid SessionId
        {
            get => _sessionId;
            set => this.RaiseAndSetIfChanged(ref _sessionId, value);
        }

        public string DeviceId
        {
            get => _deviceId;
            set => this.RaiseAndSetIfChanged(ref _deviceId, value);
        }

        public string DeviceName
        {
            get => _deviceName;
            set => this.RaiseAndSetIfChanged(ref _deviceName, value);
        }

        public DateTime ConnectedAt
        {
            get => _connectedAt;
            set => this.RaiseAndSetIfChanged(ref _connectedAt, value);
        }

        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public bool HasControl
        {
            get => _hasControl;
            set => this.RaiseAndSetIfChanged(ref _hasControl, value);
        }

        public int Latency
        {
            get => _latency;
            set => this.RaiseAndSetIfChanged(ref _latency, value);
        }

        public int Fps
        {
            get => _fps;
            set => this.RaiseAndSetIfChanged(ref _fps, value);
        }

        public string Quality
        {
            get => _quality;
            set => this.RaiseAndSetIfChanged(ref _quality, value);
        }

        public string ConnectionDuration
        {
            get
            {
                var duration = DateTime.UtcNow - ConnectedAt;
                if (duration.TotalHours >= 1)
                    return $"{(int)duration.TotalHours}h {duration.Minutes}m";
                else if (duration.TotalMinutes >= 1)
                    return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
                else
                    return $"{duration.Seconds}s";
            }
        }
    }
}