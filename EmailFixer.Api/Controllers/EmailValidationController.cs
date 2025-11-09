using EmailFixer.Api.Models;
using EmailFixer.Core.Validators;
using EmailFixer.Infrastructure.Data.Entities;
using EmailFixer.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EmailFixer.Api.Controllers;

[ApiController]
[Route("api/email")]
public class EmailValidationController : ControllerBase
{
    private readonly IEmailValidator _emailValidator;
    private readonly IUserRepository _userRepository;
    private readonly IEmailCheckRepository _emailCheckRepository;
    private readonly ILogger<EmailValidationController> _logger;

    public EmailValidationController(
        IEmailValidator emailValidator,
        IUserRepository userRepository,
        IEmailCheckRepository emailCheckRepository,
        ILogger<EmailValidationController> logger)
    {
        _emailValidator = emailValidator;
        _userRepository = userRepository;
        _emailCheckRepository = emailCheckRepository;
        _logger = logger;
    }

    /// <summary>
    /// Validates a single email address
    /// </summary>
    /// <param name="request">Email validation request</param>
    /// <returns>Validation result with remaining credits</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(EmailValidationResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 402)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> ValidateSingle([FromBody] EmailValidationRequest request)
    {
        _logger.LogInformation("Validating email {Email} for user {UserId}", request.Email, request.UserId);

        // Check user exists
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return NotFound(new ErrorResponse { Message = "User not found" });
        }

        // Check user has credits
        if (user.CreditsAvailable < 1)
        {
            _logger.LogWarning("User {UserId} has insufficient credits: {Credits}", request.UserId, user.CreditsAvailable);
            return StatusCode(402, new ErrorResponse
            {
                Message = "Insufficient credits",
                Details = $"You have {user.CreditsAvailable} credits. 1 credit required for validation."
            });
        }

        // Validate email
        var result = await _emailValidator.ValidateAsync(request.Email);

        // Deduct credit
        user.CreditsAvailable -= 1;
        user.CreditsUsed += 1;
        user.UpdatedAt = DateTime.UtcNow;
        user.LastCheckAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // Save to history
        var emailCheck = new EmailCheck
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = request.Email,
            Status = result.Status,
            Message = result.Message,
            SuggestedEmail = result.SuggestedEmail,
            MxRecordExists = result.MxRecordExists,
            SmtpCheckPassed = result.SmtpCheckPassed,
            CheckedAt = DateTime.UtcNow,
            BatchId = Guid.Empty
        };

        await _emailCheckRepository.AddAsync(emailCheck);
        await _emailCheckRepository.SaveChangesAsync();

        _logger.LogInformation("Email validation completed. Status: {Status}, Remaining credits: {Credits}",
            result.Status, user.CreditsAvailable);

        return Ok(new EmailValidationResponse
        {
            Email = request.Email,
            IsValid = result.Status == EmailFixer.Shared.Models.EmailValidationStatus.Valid,
            Status = result.Status.ToString(),
            Suggestion = result.SuggestedEmail,
            RemainingCredits = user.CreditsAvailable,
            ValidationErrors = result.ValidationErrors.Any() ? result.ValidationErrors : null
        });
    }

    /// <summary>
    /// Validates multiple email addresses in batch
    /// </summary>
    /// <param name="request">Batch validation request</param>
    /// <returns>Batch validation results with remaining credits</returns>
    [HttpPost("validate-batch")]
    [ProducesResponseType(typeof(BatchEmailValidationResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 402)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> ValidateBatch([FromBody] BatchEmailValidationRequest request)
    {
        _logger.LogInformation("Batch validating {Count} emails for user {UserId}",
            request.Emails.Count, request.UserId);

        // Validate batch size
        if (request.Emails.Count > 100)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Maximum 100 emails per batch",
                Details = $"You provided {request.Emails.Count} emails."
            });
        }

        if (request.Emails.Count == 0)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "At least one email is required"
            });
        }

        // Check user exists
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return NotFound(new ErrorResponse { Message = "User not found" });
        }

        // Check sufficient credits
        if (user.CreditsAvailable < request.Emails.Count)
        {
            _logger.LogWarning("User {UserId} has insufficient credits. Required: {Required}, Available: {Available}",
                request.UserId, request.Emails.Count, user.CreditsAvailable);

            return StatusCode(402, new ErrorResponse
            {
                Message = "Insufficient credits",
                Details = $"Required: {request.Emails.Count}, Available: {user.CreditsAvailable}"
            });
        }

        // Validate all emails
        var results = new List<EmailValidationResult>();
        var batchId = Guid.NewGuid();

        foreach (var email in request.Emails)
        {
            var validationResult = await _emailValidator.ValidateAsync(email);

            results.Add(new EmailValidationResult
            {
                Email = email,
                IsValid = validationResult.Status == EmailFixer.Shared.Models.EmailValidationStatus.Valid,
                Status = validationResult.Status.ToString(),
                Suggestion = validationResult.SuggestedEmail,
                ValidationErrors = validationResult.ValidationErrors.Any() ? validationResult.ValidationErrors : null
            });

            // Save to history
            await _emailCheckRepository.AddAsync(new EmailCheck
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = email,
                Status = validationResult.Status,
                Message = validationResult.Message,
                SuggestedEmail = validationResult.SuggestedEmail,
                MxRecordExists = validationResult.MxRecordExists,
                SmtpCheckPassed = validationResult.SmtpCheckPassed,
                CheckedAt = DateTime.UtcNow,
                BatchId = batchId
            });
        }

        // Deduct credits
        user.CreditsAvailable -= request.Emails.Count;
        user.CreditsUsed += request.Emails.Count;
        user.UpdatedAt = DateTime.UtcNow;
        user.LastCheckAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _emailCheckRepository.SaveChangesAsync();

        _logger.LogInformation("Batch validation completed. Processed: {Count}, Remaining credits: {Credits}",
            results.Count, user.CreditsAvailable);

        return Ok(new BatchEmailValidationResponse
        {
            Results = results,
            TotalProcessed = results.Count,
            CreditsUsed = request.Emails.Count,
            RemainingCredits = user.CreditsAvailable
        });
    }
}
