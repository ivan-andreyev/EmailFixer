namespace EmailFixer.Infrastructure.Data.Entities;

/// <summary>
/// Entity для транзакций кредитов
/// Поддерживает как Paddle.com так и Stripe (legacy) для backward compatibility
/// </summary>
public class CreditTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Transaction info
    public int CreditsChange { get; set; } // Positive for purchase, negative for usage
    public decimal Amount { get; set; } // Amount in USD
    public TransactionType Type { get; set; }
    public string? Description { get; set; }

    // Payment tracking
    public string? StripePaymentIntentId { get; set; } // Legacy Stripe integration (backward compatibility)
    public string? PaddleTransactionId { get; set; } // Paddle.com transaction ID
    public PaymentStatus Status { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Relations
    public User User { get; set; } = null!;
}

/// <summary>
/// Тип транзакции
/// </summary>
public enum TransactionType
{
    Purchase = 0,
    Usage = 1,
    Refund = 2,
    Bonus = 3
}

/// <summary>
/// Статус платежа
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}
