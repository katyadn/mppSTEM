namespace Microclimate.Domain.Common;

/// <summary>Перевірки інваріантів, спільні для сутностей.</summary>
internal static class Guard
{
    public static string RequiredText(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainException($"{field} є обов'язковим.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{field} не може бути довшим за {maxLength} символів.");
        }

        return trimmed;
    }

    public static string? OptionalText(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : RequiredText(trimmed, field, maxLength);
    }

    public static Guid NotEmpty(Guid value, string field)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{field} не може бути порожнім ідентифікатором.");
        }

        return value;
    }

    public static double Finite(double value, string field)
    {
        if (!double.IsFinite(value))
        {
            throw new DomainException($"{field} має бути скінченним числом.");
        }

        return value;
    }

    public static TEnum Defined<TEnum>(TEnum value, string field)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainException($"{field} має недопустиме значення '{value}'.");
        }

        return value;
    }
}
