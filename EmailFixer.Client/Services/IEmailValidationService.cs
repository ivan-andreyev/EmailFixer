using EmailFixer.Shared.Models;

namespace EmailFixer.Client.Services;

/// <summary>
/// Service for email validation operations
/// </summary>
public interface IEmailValidationService
{
    /// <summary>
    /// Validates a single email address for the specified user
    /// </summary>
    /// <param name="email">The email address to validate</param>
    /// <param name="userId">The user ID performing the validation</param>
    /// <returns>Validation result or null if validation failed or insufficient credits</returns>
    Task<EmailCheckModel?> ValidateSingleAsync(string email, Guid userId);

    /// <summary>
    /// Validates multiple email addresses in a batch for the specified user
    /// </summary>
    /// <param name="emails">List of email addresses to validate</param>
    /// <param name="userId">The user ID performing the validation</param>
    /// <returns>Batch validation result or null if validation failed or insufficient credits</returns>
    Task<EmailCheckBatchResult?> ValidateBatchAsync(List<string> emails, Guid userId);

    /// <summary>
    /// Retrieves validation history for the specified user
    /// </summary>
    /// <param name="userId">The user ID to get history for</param>
    /// <param name="skip">Number of records to skip for pagination</param>
    /// <param name="take">Number of records to take for pagination</param>
    /// <returns>List of email validation records</returns>
    Task<List<EmailCheckModel>> GetUserHistoryAsync(Guid userId, int skip = 0, int take = 20);
}
