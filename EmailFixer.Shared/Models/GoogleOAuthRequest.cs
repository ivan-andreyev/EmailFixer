namespace EmailFixer.Shared.Models;

/// <summary>
/// Request model for Google OAuth callback
/// Contains authorization code and PKCE code verifier
/// </summary>
public class GoogleOAuthRequest
{
    /// <summary>
    /// Authorization code received from Google OAuth callback
    /// </summary>
    public required string GoogleAuthorizationCode { get; set; }

    /// <summary>
    /// PKCE code verifier used to generate code_challenge
    /// </summary>
    public required string CodeVerifier { get; set; }
}
