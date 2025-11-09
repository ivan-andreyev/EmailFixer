namespace EmailFixer.Api.Models;

/// <summary>
/// Request model for single email validation
/// </summary>
public class EmailValidationRequest
{
    /// <summary>
    /// User ID making the request
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Email address to validate
    /// </summary>
    public required string Email { get; set; }
}

/// <summary>
/// Request model for batch email validation
/// </summary>
public class BatchEmailValidationRequest
{
    /// <summary>
    /// User ID making the request
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// List of email addresses to validate (max 100)
    /// </summary>
    public required List<string> Emails { get; set; }
}

/// <summary>
/// Response model for single email validation
/// </summary>
public class EmailValidationResponse
{
    /// <summary>
    /// Email address that was validated
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Whether the email is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation status
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Suggested correction (if typo detected)
    /// </summary>
    public string? Suggestion { get; set; }

    /// <summary>
    /// Remaining credits after this check
    /// </summary>
    public int RemainingCredits { get; set; }

    /// <summary>
    /// Validation errors if any
    /// </summary>
    public List<string>? ValidationErrors { get; set; }
}

/// <summary>
/// Response model for batch email validation
/// </summary>
public class BatchEmailValidationResponse
{
    /// <summary>
    /// Validation results for each email
    /// </summary>
    public required List<EmailValidationResult> Results { get; set; }

    /// <summary>
    /// Total number of emails processed
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Total credits used
    /// </summary>
    public int CreditsUsed { get; set; }

    /// <summary>
    /// Remaining credits after batch processing
    /// </summary>
    public int RemainingCredits { get; set; }
}

/// <summary>
/// Individual email validation result in batch
/// </summary>
public class EmailValidationResult
{
    /// <summary>
    /// Email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Whether the email is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation status
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Suggested correction
    /// </summary>
    public string? Suggestion { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string>? ValidationErrors { get; set; }
}

/// <summary>
/// Generic error response
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Trace ID for debugging
    /// </summary>
    public string? TraceId { get; set; }
}
