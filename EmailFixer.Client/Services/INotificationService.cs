namespace EmailFixer.Client.Services;

/// <summary>
/// Service for displaying notifications to the user
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Displays a success notification
    /// </summary>
    /// <param name="message">The success message to display</param>
    Task ShowSuccess(string message);

    /// <summary>
    /// Displays an error notification
    /// </summary>
    /// <param name="message">The error message to display</param>
    Task ShowError(string message);

    /// <summary>
    /// Displays an informational notification
    /// </summary>
    /// <param name="message">The informational message to display</param>
    Task ShowInfo(string message);

    /// <summary>
    /// Displays a warning notification
    /// </summary>
    /// <param name="message">The warning message to display</param>
    Task ShowWarning(string message);
}
