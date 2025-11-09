namespace EmailFixer.Api.Models;

/// <summary>
/// User data transfer object
/// </summary>
public class UserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Available credits
    /// </summary>
    public int Credits { get; set; }

    /// <summary>
    /// Account creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new user
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    public required string Email { get; set; }
}

/// <summary>
/// Request model for updating user credits
/// </summary>
public class UpdateCreditsRequest
{
    /// <summary>
    /// New credit balance
    /// </summary>
    public int Credits { get; set; }
}

/// <summary>
/// Email check history item DTO
/// </summary>
public class EmailCheckDto
{
    /// <summary>
    /// Check ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Email that was checked
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Whether email is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Suggested correction
    /// </summary>
    public string? Suggestion { get; set; }

    /// <summary>
    /// When the check was performed
    /// </summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// Credits used for this check
    /// </summary>
    public int CreditsUsed { get; set; }

    /// <summary>
    /// Batch ID if part of batch check
    /// </summary>
    public Guid? BatchId { get; set; }
}

/// <summary>
/// User history response with pagination
/// </summary>
public class UserHistoryResponse
{
    /// <summary>
    /// History items
    /// </summary>
    public required List<EmailCheckDto> Items { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
