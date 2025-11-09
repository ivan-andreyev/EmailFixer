using EmailFixer.Shared.Models;

namespace EmailFixer.Core.Models;

/// <summary>
/// Результат валидации email адреса
/// </summary>
public class EmailValidationResult
{
    /// <summary>
    /// Email адрес
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Статус валидации
    /// </summary>
    public EmailValidationStatus Status { get; set; }

    /// <summary>
    /// Сообщение об ошибке или предупреждение
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Ошибки валидации (может быть несколько)
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Предложенный вариант (для исправления опечаток)
    /// </summary>
    public string? SuggestedEmail { get; set; }

    /// <summary>
    /// Проверен ли MX record домена
    /// </summary>
    public bool MxRecordExists { get; set; }

    /// <summary>
    /// Проверка SMTP (опциональная, может быть null)
    /// </summary>
    public bool? SmtpCheckPassed { get; set; }
}
