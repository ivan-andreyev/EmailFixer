namespace EmailFixer.Shared.Models;

/// <summary>
/// Модель пользователя
/// </summary>
public class UserModel
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
    public int CreditsAvailable { get; set; }
    public int CreditsUsed { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastCheckAt { get; set; }
}
