using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Realtime;

/// <summary>
/// Подія реального часу: отримано свіжі виміри показників для кімнати.
/// </summary>
/// <param name="RoomId">Ідентифікатор кімнати.</param>
/// <param name="Metric">Тип фізичної метрики.</param>
/// <param name="Value">Поточне значення в одиницях метрики.</param>
/// <param name="MeasuredAt">Час вимірювання сенсором (UTC).</param>
public sealed record MeasurementReceivedEvent(
    Guid RoomId,
    MetricType Metric,
    double Value,
    DateTimeOffset MeasuredAt);
