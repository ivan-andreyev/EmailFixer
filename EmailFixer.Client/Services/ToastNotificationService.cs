using System;

namespace EmailFixer.Client.Services;

/// <summary>
/// Toast notification model
/// </summary>
public class ToastNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int DurationMs { get; set; } = 5000;
}

/// <summary>
/// Toast notification types
/// </summary>
public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

/// <summary>
/// Service for managing toast notifications
/// Replaces browser alert() with non-blocking toast notifications
/// </summary>
public interface IToastNotificationService
{
    event Action<ToastNotification>? OnNotification;
    event Action<string>? OnRemoveNotification;

    void ShowSuccess(string message, int durationMs = 5000);
    void ShowError(string message, int durationMs = 7000);
    void ShowWarning(string message, int durationMs = 6000);
    void ShowInfo(string message, int durationMs = 5000);
    void RemoveNotification(string id);
    void ClearAll();
}

public class ToastNotificationService : IToastNotificationService
{
    public event Action<ToastNotification>? OnNotification;
    public event Action<string>? OnRemoveNotification;

    public void ShowSuccess(string message, int durationMs = 5000)
    {
        Show(message, ToastType.Success, durationMs);
    }

    public void ShowError(string message, int durationMs = 7000)
    {
        Show(message, ToastType.Error, durationMs);
    }

    public void ShowWarning(string message, int durationMs = 6000)
    {
        Show(message, ToastType.Warning, durationMs);
    }

    public void ShowInfo(string message, int durationMs = 5000)
    {
        Show(message, ToastType.Info, durationMs);
    }

    public void RemoveNotification(string id)
    {
        OnRemoveNotification?.Invoke(id);
    }

    public void ClearAll()
    {
        // Signal to remove all notifications
        OnNotification?.Invoke(new ToastNotification { Id = "CLEAR_ALL" });
    }

    private void Show(string message, ToastType type, int durationMs)
    {
        var notification = new ToastNotification
        {
            Message = message,
            Type = type,
            DurationMs = durationMs
        };

        OnNotification?.Invoke(notification);
    }
}
