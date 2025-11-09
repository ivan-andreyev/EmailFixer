namespace EmailFixer.Infrastructure.Data.Entities;

/// <summary>
/// Entity для пользователя
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }

    // Credits
    public int CreditsAvailable { get; set; }
    public int CreditsUsed { get; set; }
    public decimal TotalSpent { get; set; }

    // Payment tracking
    public string? StripeCustomerId { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastCheckAt { get; set; }

    // Relations
    public ICollection<EmailCheck> EmailChecks { get; set; } = new List<EmailCheck>();
    public ICollection<CreditTransaction> Transactions { get; set; } = new List<CreditTransaction>();
}
