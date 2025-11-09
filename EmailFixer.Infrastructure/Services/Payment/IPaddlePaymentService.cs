namespace EmailFixer.Infrastructure.Services.Payment;

/// <summary>
/// Interface for Paddle.com payment processing service
/// Handles checkout creation, webhook processing, and signature validation
/// </summary>
public interface IPaddlePaymentService
{
    /// <summary>
    /// Creates a checkout URL for purchasing email credits via Paddle.com
    /// </summary>
    /// <param name="userId">User ID who is making the purchase</param>
    /// <param name="emailsCount">Number of email checks to purchase (100 checks = $1.00)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Checkout result with URL and transaction details</returns>
    Task<PaddleCheckoutResult> CreateCheckoutUrlAsync(
        Guid userId,
        int emailsCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles Paddle transaction webhook (transaction.completed, transaction.updated)
    /// Processes payment completion and credits user account
    /// </summary>
    /// <param name="webhookPayload">Raw webhook payload JSON</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Webhook processing result with transaction details</returns>
    Task<PaddleWebhookResult> HandleTransactionWebhookAsync(
        string webhookPayload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates Paddle webhook signature using HMAC-SHA256
    /// </summary>
    /// <param name="body">Raw webhook body (JSON string)</param>
    /// <param name="signature">Paddle-Signature header value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    Task<bool> ValidateWebhookSignatureAsync(
        string body,
        string signature,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of Paddle checkout creation
/// </summary>
/// <param name="CheckoutUrl">Paddle checkout URL to redirect user to</param>
/// <param name="TransactionId">Paddle transaction ID</param>
/// <param name="Amount">Payment amount in USD</param>
/// <param name="CreditsCount">Number of email credits purchased</param>
public record PaddleCheckoutResult(
    string CheckoutUrl,
    string TransactionId,
    decimal Amount,
    int CreditsCount);

/// <summary>
/// Result of Paddle webhook processing
/// </summary>
/// <param name="Success">Whether webhook was processed successfully</param>
/// <param name="TransactionId">Paddle transaction ID</param>
/// <param name="UserId">User ID who received credits</param>
/// <param name="CreditsAdded">Number of credits added to user account</param>
/// <param name="EventType">Paddle event type (transaction.completed, etc.)</param>
/// <param name="ErrorMessage">Error message if processing failed</param>
public record PaddleWebhookResult(
    bool Success,
    string? TransactionId,
    Guid? UserId,
    int CreditsAdded,
    string? EventType,
    string? ErrorMessage);
