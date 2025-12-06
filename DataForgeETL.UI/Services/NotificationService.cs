using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;

namespace DataForgeETL.UI.Services
{
    /// <summary>
    /// Service for displaying toast notifications
    /// </summary>
    public interface INotificationService
    {
        void ShowSuccess(string title, string message);
        void ShowError(string title, string message);
        void ShowWarning(string title, string message);
        void ShowInfo(string title, string message);
    }

    /// <summary>
    /// Avalonia implementation of notification service
    /// </summary>
    public class AvaloniaNotificationService : INotificationService
    {
        private WindowNotificationManager? _notificationManager;

        public void Initialize(Window window)
        {
            _notificationManager = new WindowNotificationManager(window)
            {
                Position = NotificationPosition.BottomRight,
                MaxItems = 3
            };
        }

        public void ShowSuccess(string title, string message)
        {
            Show(title, message, NotificationType.Success);
        }

        public void ShowError(string title, string message)
        {
            Show(title, message, NotificationType.Error);
        }

        public void ShowWarning(string title, string message)
        {
            Show(title, message, NotificationType.Warning);
        }

        public void ShowInfo(string title, string message)
        {
            Show(title, message, NotificationType.Information);
        }

        private void Show(string title, string message, NotificationType type)
        {
            if (_notificationManager == null)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                _notificationManager.Show(new Notification(title, message, type, TimeSpan.FromSeconds(5)));
            });
        }
    }
}
