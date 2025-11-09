namespace EmailFixer.Infrastructure.Data.Entities;

/// <summary>
/// Entity для пользователя
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }

    // OAuth authentication
    public string? GoogleId { get; set; }
    public string AuthProvider { get; set; } = "google"; // "google", "local", etc.

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
    public DateTime? LastLoginAt { get; set; }

    // Account status
    public bool IsActive { get; set; } = true;

    // Relations
    public ICollection<EmailCheck> EmailChecks { get; set; } = new List<EmailCheck>();
    public ICollection<CreditTransaction> Transactions { get; set; } = new List<CreditTransaction>();
}
