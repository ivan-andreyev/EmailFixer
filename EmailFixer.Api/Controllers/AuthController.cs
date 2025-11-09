using EmailFixer.Infrastructure.Services.Authentication;
using EmailFixer.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmailFixer.Api.Controllers;

/// <summary>
/// Authentication controller for Google OAuth and JWT token handling
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Exchanges Google authorization code for JWT token
    /// </summary>
    /// <param name="request">Code and code_verifier from frontend</param>
    /// <returns>JWT token and user info</returns>
    [HttpPost("google-callback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> GoogleCallback([FromBody] GoogleCallbackRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.CodeVerifier))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Code and code_verifier are required"
                });
            }

            // Exchange code for token
            var tokenResponse = await _authService.ExchangeCodeForTokenAsync(request.Code, request.CodeVerifier);
            if (tokenResponse?.AccessToken == null)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Failed to exchange authorization code"
                });
            }

            // Validate token and get user info
            var userInfo = await _authService.ValidateGoogleTokenAsync(tokenResponse.AccessToken);
            if (userInfo == null)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Failed to validate Google token"
                });
            }

            // Get or create user
            var user = await _authService.GetOrCreateUserFromGoogleAsync(
                userInfo.Value.Id,
                userInfo.Value.Email,
                userInfo.Value.Name);

            // Update last login
            await _authService.UpdateLastLoginAsync(user);

            // Generate JWT token
            var jwtToken = _authService.GenerateJwtToken(user);

            var response = new AuthResponse
            {
                Success = true,
                Message = "Authentication successful",
                Token = jwtToken,
                User = new UserAuthDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    GoogleId = user.GoogleId,
                    CreditsAvailable = user.CreditsAvailable,
                    CreditsUsed = user.CreditsUsed,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    IsActive = user.IsActive
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GoogleCallback: {ex.Message}");
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "An error occurred during authentication"
            });
        }
    }

    /// <summary>
    /// Returns current authenticated user info
    /// </summary>
    [HttpGet("user")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            var creditsClaimValue = User.FindFirst("CreditsAvailable")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var creditsAvailable = int.TryParse(creditsClaimValue, out var credits) ? credits : 0;

            var response = new AuthResponse
            {
                Success = true,
                User = new UserAuthDto
                {
                    Id = Guid.Parse(userIdClaim),
                    Email = emailClaim,
                    CreditsAvailable = creditsAvailable,
                    IsActive = true
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetCurrentUser: {ex.Message}");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An error occurred"
            });
        }
    }

    /// <summary>
    /// Logout endpoint (frontend clears token)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        var response = new AuthResponse
        {
            Success = true,
            Message = "Logged out successfully"
        };

        return Ok(response);
    }
}

/// <summary>
/// Request model for Google OAuth callback
/// </summary>
public class GoogleCallbackRequest
{
    public string? Code { get; set; }
    public string? CodeVerifier { get; set; }
}
