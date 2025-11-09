using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace EmailFixer.Client.Services;

/// <summary>
/// Implementation of payment service
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<PaymentService> _logger;

    /// <summary>
    /// Initializes a new instance of the PaymentService class
    /// </summary>
    /// <param name="httpClient">HTTP client for API calls</param>
    /// <param name="jsRuntime">JavaScript runtime for browser interactions</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public PaymentService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<PaymentService> logger)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Creates a checkout session for credit purchase
    /// </summary>
    /// <param name="userId">The user ID making the purchase</param>
    /// <param name="creditAmount">The number of credits to purchase</param>
    /// <returns>Checkout URL or null if session creation failed</returns>
    public async Task<string?> CreateCheckoutSessionAsync(Guid userId, int creditAmount)
    {
        try
        {
            var request = new
            {
                UserId = userId,
                Credits = creditAmount
            };

            var response = await _httpClient.PostAsJsonAsync("api/payment/create-checkout", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
                return result?.CheckoutUrl;
            }

            _logger.LogError("Failed to create checkout session with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session");
            return null;
        }
    }

    /// <summary>
    /// Verifies that a payment transaction was successful
    /// </summary>
    /// <param name="transactionId">The transaction ID to verify</param>
    /// <returns>True if payment was successful, false otherwise</returns>
    public async Task<bool> VerifyPaymentAsync(string transactionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/payment/verify/{transactionId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment {TransactionId}", transactionId);
            return false;
        }
    }

    /// <summary>
    /// Gets the purchase history for the specified user
    /// </summary>
    /// <param name="userId">The user ID to get purchase history for</param>
    /// <returns>List of purchase history records</returns>
    public async Task<List<PurchaseHistory>> GetPurchaseHistoryAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/payment/history/{userId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<PurchaseHistory>>() ?? new List<PurchaseHistory>();
            }

            _logger.LogError("Failed to get purchase history with status {StatusCode}", response.StatusCode);
            return new List<PurchaseHistory>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase history for user {UserId}", userId);
            return new List<PurchaseHistory>();
        }
    }

    private class CheckoutResponse
    {
        public string CheckoutUrl { get; set; } = string.Empty;
    }
}
