using EmailFixer.Infrastructure.Data.Entities;

namespace EmailFixer.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for CreditTransaction entity
/// </summary>
public interface ICreditTransactionRepository : IRepository<CreditTransaction>
{
    /// <summary>
    /// Gets all transactions for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user transactions ordered by creation date descending</returns>
    Task<List<CreditTransaction>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by Stripe payment intent ID
    /// </summary>
    /// <param name="paymentIntentId">Stripe payment intent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction or null if not found</returns>
    Task<CreditTransaction?> GetByStripePaymentIntentIdAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions by status
    /// </summary>
    /// <param name="status">Payment status to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions with specified status</returns>
    Task<List<CreditTransaction>> GetByStatusAsync(
        PaymentStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions by type and user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="type">Transaction type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user transactions of specified type</returns>
    Task<List<CreditTransaction>> GetByUserAndTypeAsync(
        Guid userId,
        TransactionType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by Paddle transaction ID
    /// </summary>
    /// <param name="transactionId">Paddle transaction ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction or null if not found</returns>
    Task<CreditTransaction?> GetByPaddleTransactionIdAsync(
        string transactionId,
        CancellationToken cancellationToken = default);
}
