using System.Threading.Tasks;

namespace RemoteC.Client.Services
{
    public interface ISettingsService
    {
        Task<T?> GetSettingAsync<T>(string key);
        Task SetSettingAsync<T>(string key, T value);
        Task<ClientSettings> GetAllSettingsAsync();
        Task SaveSettingsAsync(ClientSettings settings);
    }

    public class ClientSettings
    {
        public int FrameRate { get; set; } = 30;
        public string Quality { get; set; } = "High";
        public bool EnableHardwareAcceleration { get; set; } = true;
        public bool EnableE2EEncryption { get; set; } = true;
        public bool RequirePinForQuickConnect { get; set; } = true;
        public int NetworkBufferSize { get; set; } = 65536;
        public int SessionTimeout { get; set; } = 3600;
    }
}