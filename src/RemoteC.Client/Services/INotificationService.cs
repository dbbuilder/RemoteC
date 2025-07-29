using System;
using System.Threading.Tasks;

namespace RemoteC.Client.Services
{
    public interface INotificationService
    {
        Task ShowInformationAsync(string title, string message);
        Task ShowWarningAsync(string title, string message);
        Task ShowErrorAsync(string title, string message);
        Task<bool> ShowConfirmationAsync(string title, string message);
        void ShowToast(string message, NotificationType type = NotificationType.Information, int durationMs = 3000);
    }

    public enum NotificationType
    {
        Information,
        Success,
        Warning,
        Error
    }
}