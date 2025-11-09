using EmailFixer.Core.Models;

namespace EmailFixer.Core.Validators;

/// <summary>
/// Интерфейс для валидации email адресов
/// </summary>
public interface IEmailValidator
{
    /// <summary>
    /// Валидировать один email адрес
    /// </summary>
    Task<EmailValidationResult> ValidateAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Валидировать список email адресов
    /// </summary>
    Task<List<EmailValidationResult>> ValidateMultipleAsync(
        IEnumerable<string> emails,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить формат email (regex)
    /// </summary>
    bool IsValidFormat(string email);

    /// <summary>
    /// Проверить наличие MX record для домена
    /// </summary>
    Task<bool> HasMxRecordAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить, является ли это временным email (disposable email)
    /// </summary>
    bool IsDisposableEmail(string email);

    /// <summary>
    /// Найти возможные опечатки и предложить исправления
    /// </summary>
    string? SuggestCorrection(string email);
}
