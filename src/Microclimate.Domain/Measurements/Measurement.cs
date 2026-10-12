using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Metrics;
using Microclimate.Domain.Sensors;

namespace Microclimate.Domain.Measurements;

/// <summary>Один вимір метрики датчиком.</summary>
public sealed class Measurement : Entity<long>
{
    /// <summary>Допустиме відставання годинника сервера від годинника датчика.</summary>
    public static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(5);

    // Для EF Core
    private Measurement()
    {
    }

    private Measurement(Guid sensorId, MetricType metric, double value, DateTimeOffset measuredAt)
    {
        SensorId = sensorId;
        Metric = metric;
        Value = value;
        MeasuredAt = measuredAt;
    }

    public Guid SensorId { get; private set; }

    public MetricType Metric { get; private set; }

    /// <summary>Значення в одиницях метрики (див. <see cref="MetricLimits.Unit"/>).</summary>
    public double Value { get; private set; }

    /// <summary>Час виміру на датчику, UTC.</summary>
    public DateTimeOffset MeasuredAt { get; private set; }

    /// <param name="now">Поточний час сервера; вимір не може бути пізнішим за нього більше ніж на <see cref="AllowedClockSkew"/>.</param>
    public static Measurement Record(Sensor sensor, MetricType metric, double value, DateTimeOffset measuredAt, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(sensor);
        sensor.EnsureActive();
        Guard.Defined(metric, "Метрика");
        Guard.Finite(value, "Значення виміру");

        var limits = MetricLimits.For(metric);
        if (!limits.Contains(value))
        {
            throw new DomainException(
                $"Значення {value} {MetricLimits.Unit(metric)} поза фізичними межами [{limits.Min}; {limits.Max}] для {metric}.");
        }

        var measuredUtc = measuredAt.ToUniversalTime();
        if (measuredUtc > now.ToUniversalTime() + AllowedClockSkew)
        {
            throw new DomainException("Час виміру не може бути в майбутньому.");
        }

        return new Measurement(sensor.Id, metric, value, measuredUtc);
    }
}
