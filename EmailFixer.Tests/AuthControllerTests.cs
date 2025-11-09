using EmailFixer.Api.Controllers;
using EmailFixer.Infrastructure.Data.Entities;
using EmailFixer.Infrastructure.Services.Authentication;
using EmailFixer.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace EmailFixer.Tests;

/// <summary>
/// Unit tests for AuthController
/// Tests Google OAuth callback, current user retrieval, and logout endpoints
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// Test 1: AuthController_GoogleCallback_ValidCode_ReturnsAuthResponse
    /// Verifies successful Google OAuth flow with valid code and verifier
    /// </summary>
    [Fact]
    public async Task AuthController_GoogleCallback_ValidCode_ReturnsAuthResponse()
    {
        // Arrange
        var request = new GoogleCallbackRequest
        {
            Code = "valid_google_code_12345",
            CodeVerifier = "valid_code_verifier_67890"
        };

        var testUserId = Guid.NewGuid();
        var testEmail = "testuser@example.com";
        var testDisplayName = "Test User";
        var testGoogleId = "google_user_123";
        var expectedToken = "jwt_token_xyz123";

        var tokenResponse = new GoogleTokenResponse
        {
            AccessToken = "google_access_token",
            ExpiresIn = 3600,
            TokenType = "Bearer"
        };

        var userInfo = (Id: testGoogleId, Email: testEmail, Name: testDisplayName);

        var testUser = new User
        {
            Id = testUserId,
            Email = testEmail,
            DisplayName = testDisplayName,
            GoogleId = testGoogleId,
            CreditsAvailable = 100,
            CreditsUsed = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        // Setup mocks
        _authServiceMock.Setup(x => x.ExchangeCodeForTokenAsync(
                It.Is<string>(s => s == request.Code),
                It.Is<string>(s => s == request.CodeVerifier)))
            .ReturnsAsync(tokenResponse);

        _authServiceMock.Setup(x => x.ValidateGoogleTokenAsync(
                It.Is<string>(s => s == tokenResponse.AccessToken)))
            .ReturnsAsync(userInfo);

        _authServiceMock.Setup(x => x.GetOrCreateUserFromGoogleAsync(
                It.Is<string>(s => s == testGoogleId),
                It.Is<string>(s => s == testEmail),
                It.Is<string>(s => s == testDisplayName)))
            .ReturnsAsync(testUser);

        _authServiceMock.Setup(x => x.UpdateLastLoginAsync(
                It.Is<User>(u => u.Id == testUserId)))
            .Returns(Task.CompletedTask);

        _authServiceMock.Setup(x => x.GenerateJwtToken(
                It.Is<User>(u => u.Id == testUserId)))
            .Returns(expectedToken);

        // Act
        var result = await _controller.GoogleCallback(request);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);

        var authResponse = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.Equal("Authentication successful", authResponse.Message);
        Assert.Equal(expectedToken, authResponse.Token);
        Assert.NotNull(authResponse.User);
        Assert.Equal(testUserId, authResponse.User.Id);
        Assert.Equal(testEmail, authResponse.User.Email);
        Assert.Equal(testDisplayName, authResponse.User.DisplayName);
        Assert.Equal(testGoogleId, authResponse.User.GoogleId);
        Assert.Equal(100, authResponse.User.CreditsAvailable);
        Assert.Equal(0, authResponse.User.CreditsUsed);
        Assert.True(authResponse.User.IsActive);

        // Verify all services were called
        _authServiceMock.Verify(x => x.ExchangeCodeForTokenAsync(request.Code, request.CodeVerifier), Times.Once);
        _authServiceMock.Verify(x => x.ValidateGoogleTokenAsync(tokenResponse.AccessToken), Times.Once);
        _authServiceMock.Verify(x => x.GetOrCreateUserFromGoogleAsync(testGoogleId, testEmail, testDisplayName), Times.Once);
        _authServiceMock.Verify(x => x.UpdateLastLoginAsync(It.IsAny<User>()), Times.Once);
        _authServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>()), Times.Once);
    }

    /// <summary>
    /// Test 2: AuthController_GoogleCallback_InvalidCode_ReturnsBadRequest
    /// Verifies that invalid authorization code results in 400 BadRequest
    /// </summary>
    [Fact]
    public async Task AuthController_GoogleCallback_InvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var request = new GoogleCallbackRequest
        {
            Code = "invalid_code",
            CodeVerifier = "valid_verifier"
        };

        // Setup mock to return null for invalid code
        _authServiceMock.Setup(x => x.ExchangeCodeForTokenAsync(
                It.Is<string>(s => s == request.Code),
                It.Is<string>(s => s == request.CodeVerifier)))
            .ReturnsAsync((GoogleTokenResponse?)null);

        // Act
        var result = await _controller.GoogleCallback(request);

        // Assert
        Assert.NotNull(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult);
        Assert.Equal(400, badRequestResult.StatusCode);

        var authResponse = Assert.IsType<AuthResponse>(badRequestResult.Value);
        Assert.NotNull(authResponse);
        Assert.False(authResponse.Success);
        Assert.Equal("Failed to exchange authorization code", authResponse.Message);
        Assert.Null(authResponse.Token);
        Assert.Null(authResponse.User);

        // Verify ExchangeCodeForTokenAsync was called
        _authServiceMock.Verify(x => x.ExchangeCodeForTokenAsync(request.Code, request.CodeVerifier), Times.Once);

        // Verify other methods were NOT called
        _authServiceMock.Verify(x => x.ValidateGoogleTokenAsync(It.IsAny<string>()), Times.Never);
        _authServiceMock.Verify(x => x.GetOrCreateUserFromGoogleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Test 3: AuthController_GoogleCallback_MissingCodeVerifier_ReturnsBadRequest
    /// Verifies that missing code or verifier results in 400 BadRequest with validation error
    /// </summary>
    [Fact]
    public async Task AuthController_GoogleCallback_MissingCodeVerifier_ReturnsBadRequest()
    {
        // Arrange - test with null code
        var requestWithNullCode = new GoogleCallbackRequest
        {
            Code = null,
            CodeVerifier = "valid_verifier"
        };

        // Act
        var result = await _controller.GoogleCallback(requestWithNullCode);

        // Assert
        Assert.NotNull(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult);
        Assert.Equal(400, badRequestResult.StatusCode);

        var authResponse = Assert.IsType<AuthResponse>(badRequestResult.Value);
        Assert.NotNull(authResponse);
        Assert.False(authResponse.Success);
        Assert.Equal("Code and code_verifier are required", authResponse.Message);

        // Verify no service methods were called
        _authServiceMock.Verify(x => x.ExchangeCodeForTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Test 3b: Additional test for missing code verifier
    /// Verifies that missing verifier results in 400 BadRequest
    /// </summary>
    [Fact]
    public async Task AuthController_GoogleCallback_MissingVerifier_ReturnsBadRequest()
    {
        // Arrange - test with null code verifier
        var requestWithNullVerifier = new GoogleCallbackRequest
        {
            Code = "valid_code",
            CodeVerifier = null
        };

        // Act
        var result = await _controller.GoogleCallback(requestWithNullVerifier);

        // Assert
        Assert.NotNull(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult);
        Assert.Equal(400, badRequestResult.StatusCode);

        var authResponse = Assert.IsType<AuthResponse>(badRequestResult.Value);
        Assert.NotNull(authResponse);
        Assert.False(authResponse.Success);
        Assert.Equal("Code and code_verifier are required", authResponse.Message);

        // Verify no service methods were called
        _authServiceMock.Verify(x => x.ExchangeCodeForTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Test 4: AuthController_GetCurrentUser_Authorized_ReturnsUserInfo
    /// Verifies that authenticated user can retrieve their information
    /// </summary>
    [Fact]
    public void AuthController_GetCurrentUser_Authorized_ReturnsUserInfo()
    {
        // Arrange
        var testUserId = Guid.NewGuid();
        var testEmail = "authorized@example.com";
        var testCredits = 250;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, testUserId.ToString()),
            new Claim(ClaimTypes.Email, testEmail),
            new Claim("CreditsAvailable", testCredits.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Setup controller's User property
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);

        var authResponse = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.NotNull(authResponse.User);
        Assert.Equal(testUserId, authResponse.User.Id);
        Assert.Equal(testEmail, authResponse.User.Email);
        Assert.Equal(testCredits, authResponse.User.CreditsAvailable);
        Assert.True(authResponse.User.IsActive);
    }

    /// <summary>
    /// Test 5: AuthController_GetCurrentUser_Unauthorized_Returns401
    /// Verifies that request without authentication returns 401 Unauthorized
    /// </summary>
    [Fact]
    public void AuthController_GetCurrentUser_Unauthorized_Returns401()
    {
        // Arrange - create claims without NameIdentifier
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        Assert.NotNull(result);
        var unauthorizedResult = Assert.IsType<UnauthorizedResult>(result);
        Assert.NotNull(unauthorizedResult);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    /// <summary>
    /// Test 5b: Additional test for completely missing authorization
    /// Verifies that request without any claims returns 401
    /// </summary>
    [Fact]
    public void AuthController_GetCurrentUser_NoAuthHeader_Returns401()
    {
        // Arrange - no claims at all
        var identity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        Assert.NotNull(result);
        var unauthorizedResult = Assert.IsType<UnauthorizedResult>(result);
        Assert.NotNull(unauthorizedResult);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    /// <summary>
    /// Test 6: AuthController_Logout_Authorized_ReturnsSuccess
    /// Verifies that logout endpoint returns success message for authorized users
    /// </summary>
    [Fact]
    public void AuthController_Logout_Authorized_ReturnsSuccess()
    {
        // Arrange
        var testUserId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, testUserId.ToString()),
            new Claim(ClaimTypes.Email, "logout@example.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.Logout();

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);

        var authResponse = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.Equal("Logged out successfully", authResponse.Message);
    }

    /// <summary>
    /// Additional Test: AuthController_GoogleCallback_TokenValidationFails_ReturnsBadRequest
    /// Verifies that failed token validation results in 400 BadRequest
    /// </summary>
    [Fact]
    public async Task AuthController_GoogleCallback_TokenValidationFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new GoogleCallbackRequest
        {
            Code = "valid_code",
            CodeVerifier = "valid_verifier"
        };

        var tokenResponse = new GoogleTokenResponse
        {
            AccessToken = "invalid_google_token",
            ExpiresIn = 3600,
            TokenType = "Bearer"
        };

        // Setup mocks - token exchange succeeds but validation fails
        _authServiceMock.Setup(x => x.ExchangeCodeForTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(tokenResponse);

        _authServiceMock.Setup(x => x.ValidateGoogleTokenAsync(
                It.Is<string>(s => s == tokenResponse.AccessToken)))
            .ReturnsAsync(((string Id, string Email, string Name)?)null);

        // Act
        var result = await _controller.GoogleCallback(request);

        // Assert
        Assert.NotNull(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult);
        Assert.Equal(400, badRequestResult.StatusCode);

        var authResponse = Assert.IsType<AuthResponse>(badRequestResult.Value);
        Assert.NotNull(authResponse);
        Assert.False(authResponse.Success);
        Assert.Equal("Failed to validate Google token", authResponse.Message);

        // Verify services were called appropriately
        _authServiceMock.Verify(x => x.ExchangeCodeForTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _authServiceMock.Verify(x => x.ValidateGoogleTokenAsync(tokenResponse.AccessToken), Times.Once);
        _authServiceMock.Verify(x => x.GetOrCreateUserFromGoogleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
