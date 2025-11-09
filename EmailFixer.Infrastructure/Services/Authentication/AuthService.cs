using EmailFixer.Infrastructure.Data;
using EmailFixer.Infrastructure.Data.Entities;
using EmailFixer.Infrastructure.Data.Repositories;
using EmailFixer.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace EmailFixer.Infrastructure.Services.Authentication;

/// <summary>
/// Authentication service implementation for Google OAuth and JWT token management
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleOAuthSettings _googleSettings;
    private readonly JwtSettings _jwtSettings;
    private readonly IUserRepository _userRepository;
    private readonly EmailFixerDbContext _dbContext;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        HttpClient httpClient,
        IOptions<GoogleOAuthSettings> googleSettings,
        IOptions<JwtSettings> jwtSettings,
        IUserRepository userRepository,
        EmailFixerDbContext dbContext,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _googleSettings = googleSettings.Value;
        _jwtSettings = jwtSettings.Value;
        _userRepository = userRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Exchanges Google authorization code for access token
    /// </summary>
    public async Task<GoogleTokenResponse?> ExchangeCodeForTokenAsync(string code, string codeVerifier)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _googleSettings.TokenEndpoint);
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _googleSettings.ClientId ?? ""),
                new KeyValuePair<string, string>("client_secret", _googleSettings.ClientSecret ?? ""),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("code_verifier", codeVerifier),
                new KeyValuePair<string, string>("redirect_uri", _googleSettings.RedirectUri ?? ""),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });

            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to exchange code for token: {errorContent}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error exchanging code for token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Validates Google access token and retrieves user info
    /// </summary>
    public async Task<(string Id, string Email, string Name)?> ValidateGoogleTokenAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _googleSettings.UserInfoEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to validate Google token");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<JsonElement>(content);

            var id = userInfo.GetProperty("id").GetString();
            var email = userInfo.GetProperty("email").GetString();
            var name = userInfo.GetProperty("name").GetString();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(email))
                return null;

            return (id, email, name ?? "");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error validating Google token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets existing user or creates new user from Google token response
    /// </summary>
    public async Task<User> GetOrCreateUserFromGoogleAsync(string googleId, string email, string displayName)
    {
        // Try to find existing user by GoogleId
        var existingUser = await _userRepository.GetByGoogleIdAsync(googleId);
        if (existingUser != null)
        {
            return existingUser;
        }

        // Try to find by email+provider combination
        var userByEmail = await _userRepository.GetByEmailAndProviderAsync(email, "google");
        if (userByEmail != null)
        {
            // Update GoogleId if missing
            if (string.IsNullOrEmpty(userByEmail.GoogleId))
            {
                userByEmail.GoogleId = googleId;
                await _userRepository.UpdateAsync(userByEmail);
            }
            return userByEmail;
        }

        // Create new user
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            GoogleId = googleId,
            AuthProvider = "google",
            CreditsAvailable = 100, // Welcome bonus
            CreditsUsed = 0,
            TotalSpent = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(newUser);
        return newUser;
    }

    /// <summary>
    /// Generates JWT token for authenticated user
    /// </summary>
    public string GenerateJwtToken(User user)
    {
        if (string.IsNullOrEmpty(_jwtSettings.Secret))
            throw new InvalidOperationException("JWT secret is not configured");

        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email),
            new Claim("GoogleId", user.GoogleId ?? ""),
            new Claim("CreditsAvailable", user.CreditsAvailable.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Updates user's last login timestamp
    /// </summary>
    public async Task UpdateLastLoginAsync(User user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
    }
}
