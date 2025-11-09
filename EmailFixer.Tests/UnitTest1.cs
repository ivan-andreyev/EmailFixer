using EmailFixer.Core.Validators;
using EmailFixer.Shared.Models;
using FluentAssertions;

namespace EmailFixer.Tests;

/// <summary>
/// Unit tests для EmailValidator
/// </summary>
public class EmailValidatorTests
{
    private readonly EmailValidator _validator;

    public EmailValidatorTests()
    {
        _validator = new EmailValidator();
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.email+tag@example.com")]
    [InlineData("user123@subdomain.example.com")]
    public async Task ValidateAsync_ValidEmails_ReturnsValid(string email)
    {
        // Act
        var result = await _validator.ValidateAsync(email);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user name@example.com")]
    public async Task ValidateAsync_InvalidFormat_ReturnsInvalid(string email)
    {
        // Act
        var result = await _validator.ValidateAsync(email);

        // Assert
        result.Status.Should().Be(EmailValidationStatus.Invalid);
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_EmptyEmail_ReturnsInvalid()
    {
        // Act
        var result = await _validator.ValidateAsync(string.Empty);

        // Assert
        result.Status.Should().Be(EmailValidationStatus.Invalid);
        result.ValidationErrors.Should().Contain("Email адрес пустой");
    }

    [Theory]
    [InlineData("user@tempmail.com")]
    [InlineData("test@10minutemail.com")]
    [InlineData("admin@mailinator.com")]
    public async Task ValidateAsync_DisposableEmail_ReturnsSuspicious(string email)
    {
        // Act
        var result = await _validator.ValidateAsync(email);

        // Assert
        result.Status.Should().Be(EmailValidationStatus.Suspicious);
        result.ValidationErrors.Should().Contain("Временный или одноразовый email сервис");
    }

    [Fact]
    public void IsValidFormat_ValidEmail_ReturnsTrue()
    {
        // Arrange
        var email = "valid@example.com";

        // Act
        var result = _validator.IsValidFormat(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidFormat_InvalidEmail_ReturnsFalse()
    {
        // Arrange
        var email = "invalid-email";

        // Act
        var result = _validator.IsValidFormat(email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsDisposableEmail_DisposableDomain_ReturnsTrue()
    {
        // Arrange
        var email = "user@tempmail.com";

        // Act
        var result = _validator.IsDisposableEmail(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDisposableEmail_LegitimateEmail_ReturnsFalse()
    {
        // Arrange
        var email = "user@gmail.com";

        // Act
        var result = _validator.IsDisposableEmail(email);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("user@gmial.com", "user@gmail.com")]
    [InlineData("user@yahooo.com", "user@yahoo.com")]
    [InlineData("user@hotmial.com", "user@hotmail.com")]
    public void SuggestCorrection_TypoEmail_ReturnsSuggestion(string email, string expected)
    {
        // Act
        var result = _validator.SuggestCorrection(email);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SuggestCorrection_CorrectEmail_ReturnsNull()
    {
        // Arrange
        var email = "user@customcompany.com"; // Email with domain that doesn't match any common domain

        // Act
        var result = _validator.SuggestCorrection(email);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateMultipleAsync_MultipleEmails_ReturnsResults()
    {
        // Arrange
        var emails = new[] { "user@example.com", "invalid-email", "test@gmail.com" };

        // Act
        var results = await _validator.ValidateMultipleAsync(emails);

        // Assert
        results.Should().HaveCount(3);
        results[0].Status.Should().NotBe(EmailValidationStatus.Invalid);
        results[1].Status.Should().Be(EmailValidationStatus.Invalid);
        results[2].Status.Should().NotBe(EmailValidationStatus.Invalid);
    }
}