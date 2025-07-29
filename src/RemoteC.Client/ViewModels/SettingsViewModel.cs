using ReactiveUI;

namespace RemoteC.Client.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private int _frameRate = 30;
        private string _quality = "High";
        private bool _enableHardwareAcceleration = true;
        private bool _enableE2EEncryption = true;
        private bool _requirePinForQuickConnect = true;

        public int FrameRate
        {
            get => _frameRate;
            set => this.RaiseAndSetIfChanged(ref _frameRate, value);
        }

        public string Quality
        {
            get => _quality;
            set => this.RaiseAndSetIfChanged(ref _quality, value);
        }

        public bool EnableHardwareAcceleration
        {
            get => _enableHardwareAcceleration;
            set => this.RaiseAndSetIfChanged(ref _enableHardwareAcceleration, value);
        }

        public bool EnableE2EEncryption
        {
            get => _enableE2EEncryption;
            set => this.RaiseAndSetIfChanged(ref _enableE2EEncryption, value);
        }

        public bool RequirePinForQuickConnect
        {
            get => _requirePinForQuickConnect;
            set => this.RaiseAndSetIfChanged(ref _requirePinForQuickConnect, value);
        }
    }
}