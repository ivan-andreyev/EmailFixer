namespace EmailFixer.Shared.Models;

/// <summary>
/// Статус валидации email
/// </summary>
public enum EmailValidationStatus
{
    /// <summary>
    /// Email валиден
    /// </summary>
    Valid,

    /// <summary>
    /// Email невалиден
    /// </summary>
    Invalid,

    /// <summary>
    /// Email подозрительный (например, временная почта или опечатка)
    /// </summary>
    Suspicious
}
