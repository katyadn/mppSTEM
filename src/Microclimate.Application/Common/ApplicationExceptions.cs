namespace Microclimate.Application.Common;

/// <summary>Ресурс не знайдено.</summary>
public sealed class NotFoundException(string message) : Exception(message)
{
    public static NotFoundException For(string resource,
        object key) => new($"{resource} '{key}' не знайдено.");
}

/// <summary>Конфлікт зі станом даних, наприклад дубль назви.</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Некоректні вхідні параметри.</summary>
public sealed class ValidationException(IReadOnlyDictionary<string,
    string[]> errors)
    : Exception("Вхідні дані не пройшли перевірку.")
{
    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
