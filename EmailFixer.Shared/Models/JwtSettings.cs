namespace EmailFixer.Shared.Models;

/// <summary>
/// JWT configuration settings
/// </summary>
public class JwtSettings
{
    public string? Secret { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshExpirationDays { get; set; } = 7;
}
