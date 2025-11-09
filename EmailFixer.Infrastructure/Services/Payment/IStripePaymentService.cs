namespace EmailFixer.Infrastructure.Services.Payment;

/// <summary>
/// Interface for Stripe payment processing service
/// </summary>
public interface IStripePaymentService
{
    /// <summary>
    /// Creates a payment intent for purchasing email credits
    /// </summary>
    /// <param name="userId">User ID who is making the purchase</param>
    /// <param name="amount">Payment amount in USD</param>
    /// <param name="emailsCount">Number of email checks to purchase (100 checks = $1.00)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment intent ID and client secret</returns>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        Guid userId,
        decimal amount,
        int emailsCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles successful payment and credits user account
    /// </summary>
    /// <param name="paymentIntentId">Stripe payment intent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction details</returns>
    Task<TransactionResult> HandlePaymentSuccessAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates Stripe webhook signature
    /// </summary>
    /// <param name="json">Webhook payload JSON</param>
    /// <param name="stripeSignature">Stripe-Signature header value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> ValidateWebhookSignatureAsync(
        string json,
        string stripeSignature,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of payment intent creation
/// </summary>
public record PaymentIntentResult(
    string PaymentIntentId,
    string ClientSecret,
    decimal Amount,
    int CreditsCount);

/// <summary>
/// Result of transaction processing
/// </summary>
public record TransactionResult(
    Guid TransactionId,
    Guid UserId,
    int CreditsAdded,
    decimal AmountPaid,
    DateTime CompletedAt);
