namespace EmailFixer.Shared.Models;

/// <summary>
/// Google OAuth configuration
/// </summary>
public class GoogleOAuthSettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RedirectUri { get; set; }
    public string? TokenEndpoint { get; set; } = "https://oauth2.googleapis.com/token";
    public string? UserInfoEndpoint { get; set; } = "https://www.googleapis.com/oauth2/v2/userinfo";
}
