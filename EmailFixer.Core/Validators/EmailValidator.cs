using System.Text.RegularExpressions;
using DnsClient;
using EmailFixer.Core.Models;
using EmailFixer.Shared.Models;

namespace EmailFixer.Core.Validators;

/// <summary>
/// Реализация валидатора email адресов
/// </summary>
public class EmailValidator : IEmailValidator
{
    private readonly ILookupClient _dnsClient;
    private static readonly HashSet<string> CommonEmailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "yahoo.com", "outlook.com", "hotmail.com", "aol.com",
        "mail.com", "protonmail.com", "icloud.com", "mail.ru", "yandex.ru"
    };

    // Список распространённых временных email сервисов
    private static readonly HashSet<string> DisposableEmailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "tempmail.com", "10minutemail.com", "guerrillamail.com", "maildrop.cc",
        "mailinator.com", "throwaway.email", "yopmail.com", "temp-mail.org",
        "disposablemail.com", "fakeinbox.com", "trashmail.com"
    };

    // Email regex - упрощённая версия для базовой проверки формата
    private static readonly Regex EmailRegex = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100)
    );

    // Более строгая RFC 5322 версия (упрощённая)
    private static readonly Regex StrictEmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100)
    );

    public EmailValidator(ILookupClient? dnsClient = null)
    {
        _dnsClient = dnsClient ?? new LookupClient();
    }

    public async Task<EmailValidationResult> ValidateAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var result = new EmailValidationResult { Email = email };

        // Проверка пустоты
        if (string.IsNullOrWhiteSpace(email))
        {
            result.Status = EmailValidationStatus.Invalid;
            result.ValidationErrors.Add("Email адрес пустой");
            return result;
        }

        email = email.Trim();

        // Проверка формата
        if (!IsValidFormat(email))
        {
            result.Status = EmailValidationStatus.Invalid;
            result.ValidationErrors.Add("Неверный формат email адреса");
            return result;
        }

        // Проверка длины
        if (email.Length > 254)
        {
            result.Status = EmailValidationStatus.Invalid;
            result.ValidationErrors.Add("Email адрес слишком длинный (максимум 254 символа)");
            return result;
        }

        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            result.Status = EmailValidationStatus.Invalid;
            result.ValidationErrors.Add("Email должен содержать ровно один символ @");
            return result;
        }

        var localPart = parts[0];
        var domain = parts[1];

        // Проверка длины локальной части
        if (localPart.Length > 64)
        {
            result.Status = EmailValidationStatus.Invalid;
            result.ValidationErrors.Add("Локальная часть email слишком длинная (максимум 64 символа)");
            return result;
        }

        // Проверка на временный email
        if (IsDisposableEmail(email))
        {
            result.Status = EmailValidationStatus.Suspicious;
            result.Message = "Это похоже на временный email адрес";
            result.ValidationErrors.Add("Временный или одноразовый email сервис");
        }

        // Поиск опечаток
        var suggestion = SuggestCorrection(email);
        if (suggestion != null)
        {
            result.Status = EmailValidationStatus.Suspicious;
            result.SuggestedEmail = suggestion;
            result.Message = $"Возможная опечатка? Вы имели в виду: {suggestion}";
        }

        // Проверка MX record
        try
        {
            result.MxRecordExists = await HasMxRecordAsync(domain, cancellationToken);
            if (!result.MxRecordExists && result.Status != EmailValidationStatus.Suspicious)
            {
                result.Status = EmailValidationStatus.Invalid;
                result.ValidationErrors.Add($"Домен {domain} не имеет MX записей");
            }
        }
        catch (Exception ex)
        {
            // Если не можем проверить DNS, не отмечаем как невалидные
            result.ValidationErrors.Add($"Не удалось проверить MX: {ex.Message}");
        }

        // Если нет ошибок и не suspicious, то Valid
        if (result.Status == EmailValidationStatus.Valid && !result.ValidationErrors.Any())
        {
            result.Status = EmailValidationStatus.Valid;
        }

        return result;
    }

    public async Task<List<EmailValidationResult>> ValidateMultipleAsync(
        IEnumerable<string> emails,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EmailValidationResult>();
        foreach (var email in emails)
        {
            var result = await ValidateAsync(email, cancellationToken);
            results.Add(result);
        }
        return results;
    }

    public bool IsValidFormat(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // Используем строгую regex для лучшей валидации
        return StrictEmailRegex.IsMatch(email);
    }

    public async Task<bool> HasMxRecordAsync(string domain, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return false;

        try
        {
            var result = await _dnsClient.QueryAsync(
                domain,
                QueryType.MX,
                QueryClass.IN,
                cancellationToken: cancellationToken);

            return result.Answers.MxRecords().Any();
        }
        catch
        {
            // DNS query failed - в реальной жизни логируем ошибку
            return false;
        }
    }

    public bool IsDisposableEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var domain = email.Split('@').LastOrDefault();
        if (string.IsNullOrEmpty(domain))
            return false;

        return DisposableEmailDomains.Contains(domain);
    }

    public string? SuggestCorrection(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return null;

        var domain = parts[1];

        // Проверяем опечатки в известных доменах
        var suggestion = FindClosestDomain(domain);
        if (suggestion != null && !suggestion.Equals(domain, StringComparison.OrdinalIgnoreCase))
        {
            return $"{parts[0]}@{suggestion}";
        }

        return null;
    }

    private static string? FindClosestDomain(string domain)
    {
        // Простая проверка на опечатки в известных доменах
        var commonMistakes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "gmial.com", "gmail.com" },
            { "gmai.com", "gmail.com" },
            { "gmal.com", "gmail.com" },
            { "gnail.com", "gmail.com" },
            { "yahooo.com", "yahoo.com" },
            { "yaho.com", "yahoo.com" },
            { "outlok.com", "outlook.com" },
            { "outloook.com", "outlook.com" },
            { "hotmial.com", "hotmail.com" },
            { "yandex.ru", "yandex.ru" }, // Нет ошибки, но добавляем для полноты
        };

        if (commonMistakes.TryGetValue(domain, out var correctDomain))
        {
            return correctDomain;
        }

        // Алгоритм Левенштейна для поиска похожих доменов
        return FindSimilarDomain(domain);
    }

    private static string? FindSimilarDomain(string domain)
    {
        const int maxDistance = 1; // Максимальное расстояние Левенштейна
        string? bestMatch = null;
        int bestDistance = int.MaxValue;

        foreach (var knownDomain in CommonEmailDomains)
        {
            var distance = LevenshteinDistance(domain, knownDomain);
            if (distance < bestDistance && distance <= maxDistance && distance > 0)
            {
                bestDistance = distance;
                bestMatch = knownDomain;
            }
        }

        return bestMatch;
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var len1 = s1.Length;
        var len2 = s2.Length;
        var d = new int[len1 + 1, len2 + 1];

        for (int i = 0; i <= len1; i++)
            d[i, 0] = i;

        for (int j = 0; j <= len2; j++)
            d[0, j] = j;

        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[len1, len2];
    }
}
