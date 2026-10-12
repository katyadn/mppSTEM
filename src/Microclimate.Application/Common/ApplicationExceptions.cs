namespace Microclimate.Application.Common;

/// <summary>Ресурс не знайдено. Шар Api перетворює на 404 ProblemDetails.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static NotFoundException For(string resource, object key) => new($"{resource} '{key}' не знайдено.");
}

/// <summary>Конфлікт зі станом даних (дубль, видалення ресурсу з історією). Шар Api перетворює на 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException()
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Некоректні вхідні параметри сценарію. Шар Api перетворює на 400 ValidationProblemDetails.</summary>
public sealed class ValidationException : Exception
{
    public ValidationException()
        : this(new Dictionary<string, string[]>())
    {
    }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Вхідні дані не пройшли перевірку.")
    {
        Errors = errors;
    }

    /// <summary>Помилки за полями.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
