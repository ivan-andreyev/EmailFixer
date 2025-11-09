namespace EmailFixer.Shared.Models;

/// <summary>
/// Модель результата проверки email
/// </summary>
public class EmailCheckModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public EmailValidationStatus Status { get; set; }
    public string? Message { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public string? SuggestedEmail { get; set; }
    public bool MxRecordExists { get; set; }
    public bool? SmtpCheckPassed { get; set; }
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Модель для группировки результатов
/// </summary>
public class EmailCheckBatchResult
{
    public Guid BatchId { get; set; }
    public Guid UserId { get; set; }
    public int TotalEmails { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public int SuspiciousCount { get; set; }
    public List<EmailCheckModel> Results { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
