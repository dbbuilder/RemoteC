using System.Threading.Tasks;

namespace RemoteC.Client.Services
{
    public class SettingsService : ISettingsService
    {
        private ClientSettings _settings = new();

        public async Task<T?> GetSettingAsync<T>(string key)
        {
            await Task.CompletedTask;
            // TODO: Implement actual settings storage
            return default;
        }

        public async Task SetSettingAsync<T>(string key, T value)
        {
            // TODO: Implement actual settings storage
            await Task.CompletedTask;
        }

        public async Task<ClientSettings> GetAllSettingsAsync()
        {
            await Task.CompletedTask;
            return _settings;
        }

        public async Task SaveSettingsAsync(ClientSettings settings)
        {
            _settings = settings;
            // TODO: Persist to disk
            await Task.CompletedTask;
        }
    }
}