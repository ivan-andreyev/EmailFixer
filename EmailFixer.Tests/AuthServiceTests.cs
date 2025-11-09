using EmailFixer.Infrastructure.Data;
using EmailFixer.Infrastructure.Data.Entities;
using EmailFixer.Infrastructure.Data.Repositories;
using EmailFixer.Infrastructure.Services.Authentication;
using EmailFixer.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EmailFixer.Tests;

/// <summary>
/// Unit tests for AuthService
/// Tests Google OAuth token exchange, validation, user creation, and JWT generation
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IOptions<GoogleOAuthSettings>> _googleSettingsMock;
    private readonly Mock<IOptions<JwtSettings>> _jwtSettingsMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly EmailFixerDbContext _dbContext;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Setup HTTP message handler mock
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        // Setup Google OAuth settings
        _googleSettingsMock = new Mock<IOptions<GoogleOAuthSettings>>();
        _googleSettingsMock.Setup(x => x.Value).Returns(new GoogleOAuthSettings
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            RedirectUri = "https://localhost:5001/auth-callback",
            TokenEndpoint = "https://oauth2.googleapis.com/token",
            UserInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo"
        });

        // Setup JWT settings
        _jwtSettingsMock = new Mock<IOptions<JwtSettings>>();
        _jwtSettingsMock.Setup(x => x.Value).Returns(new JwtSettings
        {
            Secret = "ThisIsATestSecretKeyWithAtLeast32CharactersForHS256Algorithm",
            Issuer = "EmailFixerTestIssuer",
            Audience = "EmailFixerTestAudience",
            ExpirationMinutes = 60
        });

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<EmailFixerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new EmailFixerDbContext(options);

        // Setup repository and logger mocks
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        // Create AuthService instance
        _authService = new AuthService(
            _httpClient,
            _googleSettingsMock.Object,
            _jwtSettingsMock.Object,
            _userRepositoryMock.Object,
            _dbContext,
            _loggerMock.Object);
    }

    /// <summary>
    /// Test 1: AuthService_ExchangeCodeForToken_ValidCode_ReturnsToken
    /// Verifies that valid authorization code is successfully exchanged for Google access token
    /// </summary>
    [Fact]
    public async Task AuthService_ExchangeCodeForToken_ValidCode_ReturnsToken()
    {
        // Arrange
        var code = "valid_test_code";
        var codeVerifier = "valid_code_verifier";
        var expectedAccessToken = "test_access_token_12345";
        var expectedRefreshToken = "test_refresh_token_67890";

        var googleTokenResponse = new GoogleTokenResponse
        {
            AccessToken = expectedAccessToken,
            RefreshToken = expectedRefreshToken,
            ExpiresIn = 3600,
            TokenType = "Bearer"
        };

        var jsonResponse = JsonSerializer.Serialize(googleTokenResponse);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("oauth2.googleapis.com/token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _authService.ExchangeCodeForTokenAsync(code, codeVerifier);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedAccessToken, result.AccessToken);
        Assert.Equal(expectedRefreshToken, result.RefreshToken);
        Assert.Equal(3600, result.ExpiresIn);
        Assert.Equal("Bearer", result.TokenType);
    }

    /// <summary>
    /// Test 2: AuthService_ValidateGoogleToken_ValidToken_ReturnsUserInfo
    /// Verifies that valid Google access token is validated and user information is extracted correctly
    /// </summary>
    [Fact]
    public async Task AuthService_ValidateGoogleToken_ValidToken_ReturnsUserInfo()
    {
        // Arrange
        var accessToken = "valid_access_token";
        var expectedGoogleId = "google_user_12345";
        var expectedEmail = "test@example.com";
        var expectedName = "Test User";

        var userInfoResponse = new
        {
            id = expectedGoogleId,
            email = expectedEmail,
            name = expectedName,
            verified_email = true
        };

        var jsonResponse = JsonSerializer.Serialize(userInfoResponse);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("googleapis.com/oauth2/v2/userinfo") &&
                    req.Headers.Authorization!.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == accessToken),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _authService.ValidateGoogleTokenAsync(accessToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedGoogleId, result.Value.Id);
        Assert.Equal(expectedEmail, result.Value.Email);
        Assert.Equal(expectedName, result.Value.Name);
    }

    /// <summary>
    /// Test 3: AuthService_GetOrCreateUserFromGoogle_NewUser_CreatesUser
    /// Verifies that a new user is created with welcome credits (100) when no existing user is found
    /// </summary>
    [Fact]
    public async Task AuthService_GetOrCreateUserFromGoogle_NewUser_CreatesUser()
    {
        // Arrange
        var googleId = "new_google_user_123";
        var email = "newuser@example.com";
        var displayName = "New User";

        _userRepositoryMock.Setup(x => x.GetByGoogleIdAsync(
                It.Is<string>(s => s == googleId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock.Setup(x => x.GetByEmailAndProviderAsync(
                It.Is<string>(s => s == email),
                It.Is<string>(s => s == "google"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        User? capturedUser = null;
        _userRepositoryMock.Setup(x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, ct) => capturedUser = user)
            .ReturnsAsync((User user, CancellationToken ct) => user);

        // Act
        var result = await _authService.GetOrCreateUserFromGoogleAsync(googleId, email, displayName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Equal(displayName, result.DisplayName);
        Assert.Equal(googleId, result.GoogleId);
        Assert.Equal("google", result.AuthProvider);
        Assert.Equal(100, result.CreditsAvailable); // Welcome bonus
        Assert.Equal(0, result.CreditsUsed);
        Assert.True(result.IsActive);

        // Verify repository was called
        _userRepositoryMock.Verify(x => x.AddAsync(
            It.IsAny<User>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify captured user has correct values
        Assert.NotNull(capturedUser);
        Assert.Equal(100, capturedUser.CreditsAvailable);
    }

    /// <summary>
    /// Test 4: AuthService_GetOrCreateUserFromGoogle_ExistingUser_ReturnsUser
    /// Verifies that existing user is returned without creating duplicate when GoogleId matches
    /// </summary>
    [Fact]
    public async Task AuthService_GetOrCreateUserFromGoogle_ExistingUser_ReturnsUser()
    {
        // Arrange
        var googleId = "existing_google_user_456";
        var email = "existinguser@example.com";
        var displayName = "Existing User";

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            GoogleId = googleId,
            AuthProvider = "google",
            CreditsAvailable = 250, // Different from welcome bonus
            CreditsUsed = 50,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };

        _userRepositoryMock.Setup(x => x.GetByGoogleIdAsync(
                It.Is<string>(s => s == googleId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.GetOrCreateUserFromGoogleAsync(googleId, email, displayName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingUser.Id, result.Id);
        Assert.Equal(existingUser.Email, result.Email);
        Assert.Equal(existingUser.CreditsAvailable, result.CreditsAvailable); // Should keep existing credits
        Assert.Equal(existingUser.CreditsUsed, result.CreditsUsed);

        // Verify AddAsync was NOT called (no duplicate user created)
        _userRepositoryMock.Verify(x => x.AddAsync(
            It.IsAny<User>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Test 5: AuthService_GenerateJwtToken_ValidUser_ReturnsValidToken
    /// Verifies that JWT token is generated with all required claims (ID, Email, Credits, etc.)
    /// </summary>
    [Fact]
    public void AuthService_GenerateJwtToken_ValidUser_ReturnsValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tokentest@example.com",
            DisplayName = "Token Test User",
            GoogleId = "google_token_test_123",
            CreditsAvailable = 300,
            CreditsUsed = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var token = _authService.GenerateJwtToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Parse and verify token claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.NotNull(jwtToken);

        // Verify all required claims
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(userIdClaim);
        Assert.Equal(user.Id.ToString(), userIdClaim.Value);

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal(user.Email, emailClaim.Value);

        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal(user.DisplayName, nameClaim.Value);

        var googleIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "GoogleId");
        Assert.NotNull(googleIdClaim);
        Assert.Equal(user.GoogleId, googleIdClaim.Value);

        var creditsClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "CreditsAvailable");
        Assert.NotNull(creditsClaim);
        Assert.Equal(user.CreditsAvailable.ToString(), creditsClaim.Value);

        // Verify token expiration
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
        Assert.True(jwtToken.ValidTo <= DateTime.UtcNow.AddMinutes(61)); // 60 min + buffer
    }

    /// <summary>
    /// Test 6: AuthService_GenerateJwtToken_NoSecret_ThrowsException
    /// Verifies that InvalidOperationException is thrown when JWT secret is missing
    /// </summary>
    [Fact]
    public void AuthService_GenerateJwtToken_NoSecret_ThrowsException()
    {
        // Arrange
        var jwtSettingsWithoutSecret = new Mock<IOptions<JwtSettings>>();
        jwtSettingsWithoutSecret.Setup(x => x.Value).Returns(new JwtSettings
        {
            Secret = null, // Missing secret
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        });

        var authServiceWithoutSecret = new AuthService(
            _httpClient,
            _googleSettingsMock.Object,
            jwtSettingsWithoutSecret.Object,
            _userRepositoryMock.Object,
            _dbContext,
            _loggerMock.Object);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            DisplayName = "Test User",
            GoogleId = "google_123",
            CreditsAvailable = 100
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            authServiceWithoutSecret.GenerateJwtToken(user));

        Assert.Contains("JWT secret", exception.Message);
    }
}
