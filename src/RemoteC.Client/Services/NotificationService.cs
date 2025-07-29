using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace RemoteC.Client.Services
{
    public class NotificationService : INotificationService
    {
        public async Task ShowInformationAsync(string title, string message)
        {
            // TODO: Implement native notifications
            await Task.CompletedTask;
        }

        public async Task ShowWarningAsync(string title, string message)
        {
            await Task.CompletedTask;
        }

        public async Task ShowErrorAsync(string title, string message)
        {
            await Task.CompletedTask;
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            await Task.CompletedTask;
            return true;
        }

        public void ShowToast(string message, NotificationType type = NotificationType.Information, int durationMs = 3000)
        {
            // TODO: Implement toast notifications
        }
    }
}