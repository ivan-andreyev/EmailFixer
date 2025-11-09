using EmailFixer.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmailFixer.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for CreditTransaction entity
/// </summary>
public class CreditTransactionRepository : Repository<CreditTransaction>, ICreditTransactionRepository
{
    /// <summary>
    /// Initializes a new instance of CreditTransactionRepository
    /// </summary>
    /// <param name="context">Database context</param>
    public CreditTransactionRepository(EmailFixerDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<CreditTransaction>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CreditTransaction?> GetByStripePaymentIntentIdAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            throw new ArgumentException("Payment intent ID cannot be null or empty", nameof(paymentIntentId));
        }

        return await _dbSet
            .FirstOrDefaultAsync(
                t => t.StripePaymentIntentId == paymentIntentId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<CreditTransaction>> GetByStatusAsync(
        PaymentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<CreditTransaction>> GetByUserAndTypeAsync(
        Guid userId,
        TransactionType type,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == type)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CreditTransaction?> GetByPaddleTransactionIdAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ArgumentException("Transaction ID cannot be null or empty", nameof(transactionId));
        }

        return await _dbSet
            .FirstOrDefaultAsync(
                t => t.PaddleTransactionId == transactionId,
                cancellationToken);
    }
}
