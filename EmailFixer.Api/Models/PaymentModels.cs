namespace EmailFixer.Api.Models;

/// <summary>
/// Request for creating a Paddle checkout
/// </summary>
public class CreateCheckoutRequest
{
    /// <summary>
    /// User ID purchasing credits
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Number of email credits to purchase
    /// </summary>
    public int EmailsCount { get; set; }
}

/// <summary>
/// Response for checkout creation
/// </summary>
public class CreateCheckoutResponse
{
    /// <summary>
    /// Paddle checkout URL
    /// </summary>
    public required string CheckoutUrl { get; set; }

    /// <summary>
    /// Transaction ID
    /// </summary>
    public required string TransactionId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Number of credits
    /// </summary>
    public int CreditsCount { get; set; }
}

/// <summary>
/// Webhook processing result response
/// </summary>
public class WebhookProcessedResponse
{
    /// <summary>
    /// Whether webhook was processed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Transaction ID
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// User ID who received credits
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Credits added
    /// </summary>
    public int CreditsAdded { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
