using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;

namespace Microclimate.Domain.Thresholds;

/// <summary>Пороги метрики, задані для конкретної кімнати (замість значень за замовчуванням).</summary>
public sealed class Threshold : Entity<Guid>
{
    // Для EF Core
    private Threshold()
    {
    }

    private Threshold(Guid id, Guid roomId, MetricType metric)
    {
        Id = id;
        RoomId = roomId;
        Metric = metric;
    }

    public Guid RoomId { get; private set; }

    public MetricType Metric { get; private set; }

    public double? CriticalMin { get; private set; }

    public double? WarningMin { get; private set; }

    public double? WarningMax { get; private set; }

    public double? CriticalMax { get; private set; }

    public ThresholdLimits Limits => new(CriticalMin, WarningMin, WarningMax, CriticalMax);

    public static Threshold Create(Guid roomId, MetricType metric, ThresholdLimits limits, DateTimeOffset now)
    {
        var threshold = new Threshold(Guid.CreateVersion7(now), Guard.NotEmpty(roomId, "Кімната"), Guard.Defined(metric, "Метрика"));
        threshold.Update(limits);
        return threshold;
    }

    public void Update(ThresholdLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        CriticalMin = limits.CriticalMin;
        WarningMin = limits.WarningMin;
        WarningMax = limits.WarningMax;
        CriticalMax = limits.CriticalMax;
    }

    public AlertLevel? Evaluate(double value) => Limits.Evaluate(value);
}
