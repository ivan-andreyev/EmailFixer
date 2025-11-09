namespace EmailFixer.Shared.Models;

/// <summary>
/// Response from authentication endpoints
/// </summary>
public class AuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public UserAuthDto? User { get; set; }
}

/// <summary>
/// User data returned in auth responses
/// </summary>
public class UserAuthDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? GoogleId { get; set; }
    public int CreditsAvailable { get; set; }
    public int CreditsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
}
