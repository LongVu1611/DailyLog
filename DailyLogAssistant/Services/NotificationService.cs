using System.Windows;

namespace DailyLogAssistant.Services;

public sealed class NotificationService : INotificationService
{
    public event EventHandler<NotificationEventArgs>? NotificationRequested;

    public void Show(string title, string message) =>
        NotificationRequested?.Invoke(this, new NotificationEventArgs(title, message));
}

public sealed record NotificationEventArgs(string Title, string Message);
