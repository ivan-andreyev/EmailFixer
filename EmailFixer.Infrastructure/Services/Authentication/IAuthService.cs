using EmailFixer.Infrastructure.Data.Entities;
using EmailFixer.Shared.Models;

namespace EmailFixer.Infrastructure.Services.Authentication;

/// <summary>
/// Authentication service interface for Google OAuth and JWT handling
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Exchanges Google authorization code for access token
    /// </summary>
    Task<GoogleTokenResponse?> ExchangeCodeForTokenAsync(string code, string codeVerifier);

    /// <summary>
    /// Validates Google access token and retrieves user info
    /// </summary>
    Task<(string Id, string Email, string Name)?> ValidateGoogleTokenAsync(string accessToken);

    /// <summary>
    /// Gets existing user or creates new user from Google token response
    /// </summary>
    Task<User> GetOrCreateUserFromGoogleAsync(string googleId, string email, string displayName);

    /// <summary>
    /// Generates JWT token for authenticated user
    /// </summary>
    string GenerateJwtToken(User user);

    /// <summary>
    /// Updates user's last login timestamp
    /// </summary>
    Task UpdateLastLoginAsync(User user);
}
