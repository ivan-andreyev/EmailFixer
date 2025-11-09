using EmailFixer.Api.Models;
using FluentValidation;

namespace EmailFixer.Api.Validators;

/// <summary>
/// Validator for CreateUserRequest
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
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
/// Validator for UpdateCreditsRequest
/// </summary>
public class UpdateCreditsRequestValidator : AbstractValidator<UpdateCreditsRequest>
{
    public UpdateCreditsRequestValidator()
    {
        RuleFor(x => x.Credits)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Credits cannot be negative");
    }
}
