using EmailFixer.Api.Models;
using FluentValidation;

namespace EmailFixer.Api.Validators;

/// <summary>
/// Validator for EmailValidationRequest
/// </summary>
public class EmailValidationRequestValidator : AbstractValidator<EmailValidationRequest>
{
    public EmailValidationRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(254)
            .WithMessage("Email cannot exceed 254 characters");
    }
}

/// <summary>
/// Validator for BatchEmailValidationRequest
/// </summary>
public class BatchEmailValidationRequestValidator : AbstractValidator<BatchEmailValidationRequest>
{
    public BatchEmailValidationRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Emails)
            .NotEmpty()
            .WithMessage("Email list cannot be empty")
            .Must(x => x.Count <= 100)
            .WithMessage("Maximum 100 emails per batch")
            .Must(x => x.Count >= 1)
            .WithMessage("At least one email is required");

        RuleForEach(x => x.Emails)
            .NotEmpty()
            .WithMessage("Email cannot be empty")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(254)
            .WithMessage("Email cannot exceed 254 characters");
    }
}
