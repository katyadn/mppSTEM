namespace Microclimate.Contracts.Common;

/// <summary>
/// Інтервал агрегації для побудови часових рядів аналітики.
/// </summary>
public enum AggregationInterval
{
    /// <summary>Погодинна агрегація.</summary>
    Hour = 1,

    /// <summary>Подобова агрегація.</summary>
    Day = 2,
}
