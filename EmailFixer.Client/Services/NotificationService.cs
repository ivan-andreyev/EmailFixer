using Microsoft.JSInterop;

namespace EmailFixer.Client.Services;

/// <summary>
/// Implementation of notification service using JavaScript interop
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Initializes a new instance of the NotificationService class
    /// </summary>
    /// <param name="jsRuntime">JavaScript runtime for browser interactions</param>
    public NotificationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Displays a success notification
    /// </summary>
    /// <param name="message">The success message to display</param>
    public async Task ShowSuccess(string message)
    {
        await _jsRuntime.InvokeVoidAsync("console.log", $"SUCCESS: {message}");
    }

    /// <summary>
    /// Displays an error notification
    /// </summary>
    /// <param name="message">The error message to display</param>
    public async Task ShowError(string message)
    {
        await _jsRuntime.InvokeVoidAsync("console.error", $"ERROR: {message}");
        await _jsRuntime.InvokeVoidAsync("alert", message);
    }

    /// <summary>
    /// Displays an informational notification
    /// </summary>
    /// <param name="message">The informational message to display</param>
    public async Task ShowInfo(string message)
    {
        await _jsRuntime.InvokeVoidAsync("console.info", $"INFO: {message}");
    }

    /// <summary>
    /// Displays a warning notification
    /// </summary>
    /// <param name="message">The warning message to display</param>
    public async Task ShowWarning(string message)
    {
        await _jsRuntime.InvokeVoidAsync("console.warn", $"WARNING: {message}");
    }
}
