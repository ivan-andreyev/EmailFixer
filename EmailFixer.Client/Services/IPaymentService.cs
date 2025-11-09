namespace EmailFixer.Client.Services;

/// <summary>
/// Service for payment and purchase operations
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a checkout session for credit purchase
    /// </summary>
    /// <param name="userId">The user ID making the purchase</param>
    /// <param name="creditAmount">The number of credits to purchase</param>
    /// <returns>Checkout URL or null if session creation failed</returns>
    Task<string?> CreateCheckoutSessionAsync(Guid userId, int creditAmount);

    /// <summary>
    /// Verifies that a payment transaction was successful
    /// </summary>
    /// <param name="transactionId">The transaction ID to verify</param>
    /// <returns>True if payment was successful, false otherwise</returns>
    Task<bool> VerifyPaymentAsync(string transactionId);

    /// <summary>
    /// Gets the purchase history for the specified user
    /// </summary>
    /// <param name="userId">The user ID to get purchase history for</param>
    /// <returns>List of purchase history records</returns>
    Task<List<PurchaseHistory>> GetPurchaseHistoryAsync(Guid userId);
}

/// <summary>
/// Represents a purchase history record
/// </summary>
public class PurchaseHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the purchase
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the number of credits purchased
    /// </summary>
    public int Credits { get; set; }

    /// <summary>
    /// Gets or sets the purchase amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the purchase date and time
    /// </summary>
    public DateTime PurchasedAt { get; set; }

    /// <summary>
    /// Gets or sets the payment status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
