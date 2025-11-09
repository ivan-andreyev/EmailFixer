using EmailFixer.Api.Models;
using EmailFixer.Infrastructure.Data.Entities;
using EmailFixer.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmailFixer.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailCheckRepository _emailCheckRepository;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserRepository userRepository,
        IEmailCheckRepository emailCheckRepository,
        ILogger<UserController> logger)
    {
        _userRepository = userRepository;
        _emailCheckRepository = emailCheckRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        _logger.LogInformation("Getting user {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound(new ErrorResponse { Message = "User not found" });
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Credits = user.CreditsAvailable,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        _logger.LogInformation("Creating user with email {Email}", request.Email);

        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("User with email {Email} already exists", request.Email);
            return Conflict(new ErrorResponse
            {
                Message = "User already exists",
                Details = $"A user with email {request.Email} already exists."
            });
        }

        // Create new user with free trial credits
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            CreditsAvailable = 10, // Free trial credits
            CreditsUsed = 0,
            TotalSpent = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created with {Credits} free credits", user.Id, user.CreditsAvailable);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Credits = user.CreditsAvailable,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    /// <summary>
    /// Get user credit balance
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Credit balance</returns>
    [HttpGet("{id}/credits")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetCredits(Guid id)
    {
        _logger.LogInformation("Getting credits for user {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound(new ErrorResponse { Message = "User not found" });
        }

        return Ok(new { credits = user.CreditsAvailable });
    }

    /// <summary>
    /// Update user credit balance
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Update credits request</param>
    /// <returns>Updated credit balance</returns>
    [HttpPut("{id}/credits")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> UpdateCredits(Guid id, [FromBody] UpdateCreditsRequest request)
    {
        _logger.LogInformation("Updating credits for user {UserId} to {Credits}", id, request.Credits);

        if (request.Credits < 0)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Credits cannot be negative",
                Details = $"Provided credits: {request.Credits}"
            });
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound(new ErrorResponse { Message = "User not found" });
        }

        user.CreditsAvailable = request.Credits;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("Credits updated for user {UserId}. New balance: {Credits}", id, user.CreditsAvailable);

        return Ok(new { credits = user.CreditsAvailable });
    }

    /// <summary>
    /// Get user's email validation history
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated history of email validations</returns>
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(UserHistoryResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        _logger.LogInformation("Getting history for user {UserId}, page {Page}, pageSize {PageSize}",
            id, page, pageSize);

        // Validate pagination parameters
        if (page < 1)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Page number must be greater than 0",
                Details = $"Provided page: {page}"
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Page size must be between 1 and 100",
                Details = $"Provided page size: {pageSize}"
            });
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound(new ErrorResponse { Message = "User not found" });
        }

        var history = await _emailCheckRepository.GetByUserIdAsync(id, page, pageSize);
        var totalCount = await _emailCheckRepository.GetCheckCountByUserAsync(id);

        _logger.LogInformation("Retrieved {Count} history items for user {UserId}", history.Count, id);

        return Ok(new UserHistoryResponse
        {
            Items = history.Select(h => new EmailCheckDto
            {
                Id = h.Id,
                Email = h.Email,
                IsValid = h.Status == EmailFixer.Shared.Models.EmailValidationStatus.Valid,
                Suggestion = h.SuggestedEmail,
                CheckedAt = h.CheckedAt,
                CreditsUsed = 1, // Each check costs 1 credit
                BatchId = h.BatchId == Guid.Empty ? null : h.BatchId
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount
        });
    }
}
