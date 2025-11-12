using Microsoft.JSInterop;

namespace EmailFixer.Client.Services;

/// <summary>
/// Implementation of notification service using toast notifications instead of blocking alert()
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IToastNotificationService _toastService;

    /// <summary>
    /// Initializes a new instance of the NotificationService class
    /// </summary>
    /// <param name="jsRuntime">JavaScript runtime for browser interactions</param>
    /// <param name="toastService">Toast notification service</param>
    public NotificationService(IJSRuntime jsRuntime, IToastNotificationService toastService)
    {
        _jsRuntime = jsRuntime;
        _toastService = toastService;
    }

    /// <summary>
    /// Displays a success notification (non-blocking toast)
    /// </summary>
    /// <param name="message">The success message to display</param>
    public async Task ShowSuccess(string message)
    {
        await _jsRuntime.InvokeVoidAsync("console.log", $"SUCCESS: {message}");
        _toastService.ShowSuccess(message);
    }

    /// <summary>
    /// Displays an error notification (non-blocking toast)
    /// Replaces blocking alert() with async toast
    /// </summary>
    /// <param name="message">The error message to display</param>
    public async Task ShowError(string message)
    {
        await _jsRuntime.InvokeVoidAsync("console.error", $"ERROR: {message}");
        _toastService.ShowError(message);
    }

    /// <summary>
    /// Displays an informational notification (non-blocking toast)
    /// </summary>
    /// <param name="message">The informational message to display</param>
    public async Task ShowInfo(string message)
    {
        await _jsRuntime.InvokeVoidAsync("console.info", $"INFO: {message}");
        _toastService.ShowInfo(message);
    }

    /// <summary>
    /// Displays a warning notification (non-blocking toast)
    /// </summary>
    /// <param name="message">The warning message to display</param>
    public async Task ShowWarning(string message)
    {
        await _jsRuntime.InvokeVoidAsync("console.warn", $"WARNING: {message}");
        _toastService.ShowWarning(message);
    }
}
