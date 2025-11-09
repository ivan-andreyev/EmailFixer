using EmailFixer.Shared.Models;

namespace EmailFixer.Client.Services;

/// <summary>
/// Authentication service interface for managing user authentication
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Initiates Google OAuth login flow by redirecting to Google authorization page
    /// </summary>
    Task InitiateGoogleLoginAsync();

    /// <summary>
    /// Handles Google OAuth callback by exchanging authorization code for JWT token
    /// </summary>
    /// <param name="code">Authorization code from Google</param>
    /// <param name="codeVerifier">PKCE code verifier</param>
    /// <returns>True if authentication successful, false otherwise</returns>
    Task<bool> HandleGoogleCallbackAsync(string code, string codeVerifier);

    /// <summary>
    /// Checks if user is currently authenticated
    /// </summary>
    /// <returns>True if user has valid token, false otherwise</returns>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    /// <returns>Current user or null if not authenticated</returns>
    Task<UserAuthDto?> GetCurrentUserAsync();

    /// <summary>
    /// Logs out the current user by clearing token and localStorage
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Gets the current JWT token
    /// </summary>
    /// <returns>JWT token or null if not authenticated</returns>
    Task<string?> GetTokenAsync();
}
