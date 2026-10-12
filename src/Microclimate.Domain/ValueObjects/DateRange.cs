using Microclimate.Domain.Common;

namespace Microclimate.Domain.ValueObjects;

/// <summary>Напіввідкритий інтервал часу [From, To) в UTC.</summary>
public sealed record DateRange
{
    private DateRange(DateTimeOffset from, DateTimeOffset to)
    {
        From = from;
        To = to;
    }

    /// <summary>Початок (включно), UTC.</summary>
    public DateTimeOffset From { get; }

    /// <summary>Кінець (не включно), UTC.</summary>
    public DateTimeOffset To { get; }

    /// <summary>Тривалість інтервалу.</summary>
    public TimeSpan Duration => To - From;

    /// <summary>Створює інтервал; <paramref name="from"/> має бути раніше за <paramref name="to"/>.</summary>
    public static DateRange Create(DateTimeOffset from, DateTimeOffset to)
    {
        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();
        if (fromUtc >= toUtc)
        {
            throw new DomainException("Початок періоду має бути раніше за його кінець.");
        }

        return new DateRange(fromUtc, toUtc);
    }

    /// <summary>Створює інтервал і перевіряє, що він не довший за <paramref name="maxDuration"/>.</summary>
    public static DateRange Create(DateTimeOffset from, DateTimeOffset to, TimeSpan maxDuration)
    {
        var range = Create(from, to);
        if (range.Duration > maxDuration)
        {
            throw new DomainException($"Період не може перевищувати {maxDuration.TotalDays:0.##} дн.");
        }

        return range;
    }

    /// <summary>Чи належить момент часу інтервалу.</summary>
    public bool Contains(DateTimeOffset moment) => moment >= From && moment < To;
}
