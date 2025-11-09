using System.Net.Http.Json;
using EmailFixer.Shared.Models;

namespace EmailFixer.Client.Services;

/// <summary>
/// Implementation of email validation service
/// </summary>
public class EmailValidationService : IEmailValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmailValidationService> _logger;

    /// <summary>
    /// Initializes a new instance of the EmailValidationService class
    /// </summary>
    /// <param name="httpClient">HTTP client for API calls</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public EmailValidationService(HttpClient httpClient, ILogger<EmailValidationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Validates a single email address for the specified user
    /// </summary>
    /// <param name="email">The email address to validate</param>
    /// <param name="userId">The user ID performing the validation</param>
    /// <returns>Validation result or null if validation failed or insufficient credits</returns>
    public async Task<EmailCheckModel?> ValidateSingleAsync(string email, Guid userId)
    {
        try
        {
            var request = new
            {
                UserId = userId,
                Email = email
            };

            var response = await _httpClient.PostAsJsonAsync("api/email/validate", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EmailCheckModel>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.PaymentRequired)
            {
                _logger.LogWarning("Insufficient credits for user {UserId}", userId);
                return null;
            }

            _logger.LogError("Validation failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email {Email}", email);
            return null;
        }
    }

    /// <summary>
    /// Validates multiple email addresses in a batch for the specified user
    /// </summary>
    /// <param name="emails">List of email addresses to validate</param>
    /// <param name="userId">The user ID performing the validation</param>
    /// <returns>Batch validation result or null if validation failed or insufficient credits</returns>
    public async Task<EmailCheckBatchResult?> ValidateBatchAsync(List<string> emails, Guid userId)
    {
        try
        {
            var request = new
            {
                UserId = userId,
                Emails = emails
            };

            var response = await _httpClient.PostAsJsonAsync("api/email/validate-batch", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EmailCheckBatchResult>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.PaymentRequired)
            {
                _logger.LogWarning("Insufficient credits for batch validation. User {UserId}", userId);
                return null;
            }

            _logger.LogError("Batch validation failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch validation for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Retrieves validation history for the specified user
    /// </summary>
    /// <param name="userId">The user ID to get history for</param>
    /// <param name="skip">Number of records to skip for pagination</param>
    /// <param name="take">Number of records to take for pagination</param>
    /// <returns>List of email validation records</returns>
    public async Task<List<EmailCheckModel>> GetUserHistoryAsync(Guid userId, int skip = 0, int take = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/email/history/{userId}?skip={skip}&take={take}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<EmailCheckModel>>() ?? new List<EmailCheckModel>();
            }

            _logger.LogError("Failed to get history with status {StatusCode}", response.StatusCode);
            return new List<EmailCheckModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting history for user {UserId}", userId);
            return new List<EmailCheckModel>();
        }
    }
}
