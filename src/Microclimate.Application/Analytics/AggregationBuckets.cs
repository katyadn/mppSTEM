namespace Microclimate.Application.Analytics;

/// <summary>Межі інтервалів часового ряду в UTC.</summary>
public static class AggregationBuckets
{
    /// <summary>Початок інтервалу (години або доби), до якого належить момент часу.</summary>
    public static DateTimeOffset Floor(DateTimeOffset moment, AggregationInterval interval)
    {
        var utc = moment.ToUniversalTime();
        return interval switch
        {
            AggregationInterval.Hour => new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero),
            AggregationInterval.Day => new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero),
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Невідомий інтервал."),
        };
    }

    public static TimeSpan Step(AggregationInterval interval) => interval switch
    {
        AggregationInterval.Hour => TimeSpan.FromHours(1),
        AggregationInterval.Day => TimeSpan.FromDays(1),
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Невідомий інтервал."),
    };
}
