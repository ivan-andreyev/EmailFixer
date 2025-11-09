using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using EmailFixer.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmailFixer.Client.Services;

/// <summary>
/// Authentication service implementation for Google OAuth and JWT token management
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigationManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    private const string AUTH_TOKEN_KEY = "auth_token";
    private const string AUTH_USER_KEY = "auth_user";
    private const string CODE_VERIFIER_KEY = "code_verifier";

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        NavigationManager navigationManager,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _navigationManager = navigationManager;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Initiates Google OAuth login flow with PKCE
    /// </summary>
    public async Task InitiateGoogleLoginAsync()
    {
        try
        {
            // Generate PKCE code verifier and challenge
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            // Store code verifier for later use in callback
            await _localStorage.SetItemAsStringAsync(CODE_VERIFIER_KEY, codeVerifier);

            // Get Google OAuth configuration
            var clientId = _configuration["GoogleOAuth:ClientId"];
            var redirectUri = _configuration["GoogleOAuth:RedirectUri"];
            var scope = "openid profile email";

            // Build Google OAuth authorization URL
            var authUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={Uri.EscapeDataString(clientId ?? "")}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri ?? "")}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                $"&code_challenge_method=S256" +
                $"&access_type=offline" +
                $"&prompt=consent";

            // Redirect to Google OAuth
            _navigationManager.NavigateTo(authUrl, forceLoad: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating Google login");
            throw;
        }
    }

    /// <summary>
    /// Handles Google OAuth callback by exchanging code for JWT token
    /// </summary>
    public async Task<bool> HandleGoogleCallbackAsync(string code, string codeVerifier)
    {
        try
        {
            // Create request with authorization code and code verifier
            var request = new GoogleOAuthRequest
            {
                GoogleAuthorizationCode = code,
                CodeVerifier = codeVerifier
            };

            // Send to backend API
            var response = await _httpClient.PostAsJsonAsync("api/auth/google-callback", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google callback failed: {StatusCode}", response.StatusCode);
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse == null || !authResponse.Success || string.IsNullOrEmpty(authResponse.Token))
            {
                _logger.LogError("Invalid auth response: {Message}", authResponse?.Message);
                return false;
            }

            // Store JWT token and user in localStorage
            await _localStorage.SetItemAsStringAsync(AUTH_TOKEN_KEY, authResponse.Token);
            await _localStorage.SetItemAsync(AUTH_USER_KEY, authResponse.User);

            // Clear code verifier
            await _localStorage.RemoveItemAsync(CODE_VERIFIER_KEY);

            _logger.LogInformation("User authenticated successfully: {Email}", authResponse.User?.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Google callback");
            return false;
        }
    }

    /// <summary>
    /// Checks if user is authenticated
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync(AUTH_TOKEN_KEY);
            return !string.IsNullOrEmpty(token);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    public async Task<UserAuthDto?> GetCurrentUserAsync()
    {
        try
        {
            var user = await _localStorage.GetItemAsync<UserAuthDto>(AUTH_USER_KEY);
            return user;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Logs out the current user
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync(AUTH_TOKEN_KEY);
            await _localStorage.RemoveItemAsync(AUTH_USER_KEY);
            await _localStorage.RemoveItemAsync(CODE_VERIFIER_KEY);

            _logger.LogInformation("User logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    /// <summary>
    /// Gets the current JWT token
    /// </summary>
    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _localStorage.GetItemAsStringAsync(AUTH_TOKEN_KEY);
        }
        catch
        {
            return null;
        }
    }

    #region PKCE Helpers

    /// <summary>
    /// Generates a cryptographically secure random code verifier for PKCE
    /// </summary>
    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Generates code challenge from code verifier using SHA256
    /// </summary>
    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = Encoding.UTF8.GetBytes(codeVerifier);
        var hash = SHA256.HashData(bytes);
        return Base64UrlEncode(hash);
    }

    /// <summary>
    /// Base64 URL encoding (RFC 4648 Section 5)
    /// </summary>
    private static string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    #endregion
}
