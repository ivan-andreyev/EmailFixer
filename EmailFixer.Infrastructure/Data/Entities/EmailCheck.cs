using EmailFixer.Shared.Models;

namespace EmailFixer.Infrastructure.Data.Entities;

/// <summary>
/// Entity для результата проверки email
/// </summary>
public class EmailCheck
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Email { get; set; }

    // Validation result
    public EmailValidationStatus Status { get; set; }
    public string? Message { get; set; }
    public string ValidationErrors { get; set; } = string.Empty; // JSON or delimited
    public string? SuggestedEmail { get; set; }

    // Domain check
    public bool MxRecordExists { get; set; }
    public bool? SmtpCheckPassed { get; set; }

    // Timestamps
    public DateTime CheckedAt { get; set; }

    // Batch info
    public Guid BatchId { get; set; }

    // Relations
    public User User { get; set; } = null!;
}
