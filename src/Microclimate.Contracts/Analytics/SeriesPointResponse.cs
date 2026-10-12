namespace Microclimate.Contracts.Analytics;

/// <summary>
/// Агрегована точка часового ряду (кошик інтервалу).
/// </summary>
/// <param name="BucketStart">Початок кошика агрегації в UTC.</param>
/// <param name="Count">Кількість вимірів у кошику (якщо 0, агрегати є null).</param>
/// <param name="Avg">Середнє значення за кошик.</param>
/// <param name="Min">Мінімальне значення за кошик.</param>
/// <param name="Max">Максимальне значення за кошик.</param>
public sealed record SeriesPointResponse(
    DateTimeOffset BucketStart,
    int Count,
    double? Avg,
    double? Min,
    double? Max);
