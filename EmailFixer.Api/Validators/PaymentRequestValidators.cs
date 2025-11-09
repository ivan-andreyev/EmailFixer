using EmailFixer.Api.Models;
using FluentValidation;

namespace EmailFixer.Api.Validators;

/// <summary>
/// Validator for CreateCheckoutRequest
/// </summary>
public class CreateCheckoutRequestValidator : AbstractValidator<CreateCheckoutRequest>
{
    public CreateCheckoutRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.EmailsCount)
            .GreaterThanOrEqualTo(100)
            .WithMessage("Minimum purchase is 100 email credits")
            .LessThanOrEqualTo(100000)
            .WithMessage("Maximum purchase is 100,000 email credits");
    }
}
