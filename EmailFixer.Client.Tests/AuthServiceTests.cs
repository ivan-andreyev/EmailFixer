using System.Net;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using EmailFixer.Client.Services;
using EmailFixer.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace EmailFixer.Client.Tests;

/// <summary>
/// Unit tests for frontend AuthService implementation
/// Tests Google OAuth callback handling, authentication state, and token management
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly Mock<NavigationManager> _navigationManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    // Test data
    private const string TEST_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.token";
    private const string TEST_CODE = "test_google_auth_code";
    private const string TEST_CODE_VERIFIER = "test_code_verifier";
    private readonly UserAuthDto _testUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        DisplayName = "Test User",
        GoogleId = "google123",
        CreditsAvailable = 100,
        CreditsUsed = 0,
        CreatedAt = DateTime.UtcNow,
        LastLoginAt = DateTime.UtcNow,
        IsActive = true
    };

    public AuthServiceTests()
    {
        // Setup mocks
        _localStorageMock = new Mock<ILocalStorageService>();
        _navigationManagerMock = new Mock<NavigationManager>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        // Setup HttpClient with mocked handler
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost:5001/")
        };

        // Setup configuration defaults
        _configurationMock.Setup(c => c["GoogleOAuth:ClientId"]).Returns("test-client-id");
        _configurationMock.Setup(c => c["GoogleOAuth:RedirectUri"]).Returns("https://localhost:5001/auth/callback");

        // Create AuthService instance
        _authService = new AuthService(
            _httpClient,
            _localStorageMock.Object,
            _navigationManagerMock.Object,
            _configurationMock.Object,
            _loggerMock.Object
        );
    }

    /// <summary>
    /// Test 1: AuthService_HandleGoogleCallback_ValidCode_SavesTokenToStorage
    /// Verifies that valid Google OAuth code results in token and user data being saved to localStorage
    /// </summary>
    [Fact]
    public async Task AuthService_HandleGoogleCallback_ValidCode_SavesTokenToStorage()
    {
        // Arrange
        var authResponse = new AuthResponse
        {
            Success = true,
            Message = "Authentication successful",
            Token = TEST_TOKEN,
            User = _testUser
        };

        // Mock successful HTTP response
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("api/auth/google-callback")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(authResponse)
            });

        string? savedToken = null;
        UserAuthDto? savedUser = null;

        _localStorageMock
            .Setup(ls => ls.SetItemAsStringAsync("auth_token", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((key, value, ct) => savedToken = value)
            .Returns(ValueTask.CompletedTask);

        _localStorageMock
            .Setup(ls => ls.SetItemAsync("auth_user", It.IsAny<UserAuthDto>(), It.IsAny<CancellationToken>()))
            .Callback<string, UserAuthDto, CancellationToken>((key, value, ct) => savedUser = value)
            .Returns(ValueTask.CompletedTask);

        _localStorageMock
            .Setup(ls => ls.RemoveItemAsync("code_verifier", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _authService.HandleGoogleCallbackAsync(TEST_CODE, TEST_CODE_VERIFIER);

        // Assert
        result.Should().BeTrue("valid code should result in successful authentication");

        savedToken.Should().NotBeNull("token should be saved to localStorage");
        savedToken.Should().Be(TEST_TOKEN, "saved token should match the token from auth response");

        savedUser.Should().NotBeNull("user data should be saved to localStorage");
        savedUser.Should().BeEquivalentTo(_testUser, "saved user should match the user from auth response");

        _localStorageMock.Verify(
            ls => ls.SetItemAsStringAsync("auth_token", TEST_TOKEN, It.IsAny<CancellationToken>()),
            Times.Once,
            "token should be saved to localStorage exactly once"
        );

        _localStorageMock.Verify(
            ls => ls.SetItemAsync("auth_user", It.IsAny<UserAuthDto>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "user should be saved to localStorage exactly once"
        );

        _localStorageMock.Verify(
            ls => ls.RemoveItemAsync("code_verifier", It.IsAny<CancellationToken>()),
            Times.Once,
            "code verifier should be removed after successful authentication"
        );
    }

    /// <summary>
    /// Test 2: AuthService_HandleGoogleCallback_InvalidCode_ReturnsFalse
    /// Verifies that invalid Google OAuth code results in failed authentication with no data saved
    /// </summary>
    [Fact]
    public async Task AuthService_HandleGoogleCallback_InvalidCode_ReturnsFalse()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("api/auth/google-callback")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"error\":\"Invalid authorization code\"}")
            });

        // Act
        var result = await _authService.HandleGoogleCallbackAsync("invalid_code", TEST_CODE_VERIFIER);

        // Assert
        result.Should().BeFalse("invalid code should result in failed authentication");

        _localStorageMock.Verify(
            ls => ls.SetItemAsStringAsync("auth_token", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "token should not be saved when authentication fails"
        );

        _localStorageMock.Verify(
            ls => ls.SetItemAsync("auth_user", It.IsAny<UserAuthDto>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "user should not be saved when authentication fails"
        );
    }

    /// <summary>
    /// Test 3: AuthService_IsAuthenticatedAsync_WithValidToken_ReturnsTrue
    /// Verifies that authentication check returns true when valid token exists in localStorage
    /// </summary>
    [Fact]
    public async Task AuthService_IsAuthenticatedAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        _localStorageMock
            .Setup(ls => ls.GetItemAsStringAsync("auth_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TEST_TOKEN);

        // Act
        var result = await _authService.IsAuthenticatedAsync();

        // Assert
        result.Should().BeTrue("should return true when valid token exists in localStorage");

        _localStorageMock.Verify(
            ls => ls.GetItemAsStringAsync("auth_token", It.IsAny<CancellationToken>()),
            Times.Once,
            "should check localStorage for token exactly once"
        );
    }

    /// <summary>
    /// Test 4: AuthService_IsAuthenticatedAsync_WithoutToken_ReturnsFalse
    /// Verifies that authentication check returns false when no token exists in localStorage
    /// </summary>
    [Fact]
    public async Task AuthService_IsAuthenticatedAsync_WithoutToken_ReturnsFalse()
    {
        // Arrange
        _localStorageMock
            .Setup(ls => ls.GetItemAsStringAsync("auth_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _authService.IsAuthenticatedAsync();

        // Assert
        result.Should().BeFalse("should return false when no token exists in localStorage");

        _localStorageMock.Verify(
            ls => ls.GetItemAsStringAsync("auth_token", It.IsAny<CancellationToken>()),
            Times.Once,
            "should check localStorage for token exactly once"
        );
    }

    /// <summary>
    /// Test 5: AuthService_LogoutAsync_ClearsStorageAndToken
    /// Verifies that logout clears all auth data from localStorage (token, user, code verifier)
    /// </summary>
    [Fact]
    public async Task AuthService_LogoutAsync_ClearsStorageAndToken()
    {
        // Arrange
        var removedKeys = new List<string>();

        _localStorageMock
            .Setup(ls => ls.RemoveItemAsync("auth_token", It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, ct) => removedKeys.Add(key))
            .Returns(ValueTask.CompletedTask);

        _localStorageMock
            .Setup(ls => ls.RemoveItemAsync("auth_user", It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, ct) => removedKeys.Add(key))
            .Returns(ValueTask.CompletedTask);

        _localStorageMock
            .Setup(ls => ls.RemoveItemAsync("code_verifier", It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, ct) => removedKeys.Add(key))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _authService.LogoutAsync();

        // Assert
        removedKeys.Should().Contain("auth_token", "token should be removed from localStorage");
        removedKeys.Should().Contain("auth_user", "user data should be removed from localStorage");
        removedKeys.Should().Contain("code_verifier", "code verifier should be removed from localStorage");
        removedKeys.Should().HaveCount(3, "exactly 3 items should be removed from localStorage");

        _localStorageMock.Verify(
            ls => ls.RemoveItemAsync("auth_token", It.IsAny<CancellationToken>()),
            Times.Once,
            "auth_token should be removed exactly once"
        );

        _localStorageMock.Verify(
            ls => ls.RemoveItemAsync("auth_user", It.IsAny<CancellationToken>()),
            Times.Once,
            "auth_user should be removed exactly once"
        );

        _localStorageMock.Verify(
            ls => ls.RemoveItemAsync("code_verifier", It.IsAny<CancellationToken>()),
            Times.Once,
            "code_verifier should be removed exactly once"
        );
    }

    /// <summary>
    /// Test 6: AuthService_GetTokenAsync_WithValidToken_ReturnsToken
    /// Verifies that GetTokenAsync returns the token from localStorage when it exists
    /// </summary>
    [Fact]
    public async Task AuthService_GetTokenAsync_WithValidToken_ReturnsToken()
    {
        // Arrange
        _localStorageMock
            .Setup(ls => ls.GetItemAsStringAsync("auth_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TEST_TOKEN);

        // Act
        var result = await _authService.GetTokenAsync();

        // Assert
        result.Should().NotBeNull("token should be returned when it exists in localStorage");
        result.Should().Be(TEST_TOKEN, "returned token should match the token stored in localStorage");

        _localStorageMock.Verify(
            ls => ls.GetItemAsStringAsync("auth_token", It.IsAny<CancellationToken>()),
            Times.Once,
            "should retrieve token from localStorage exactly once"
        );
    }
}
