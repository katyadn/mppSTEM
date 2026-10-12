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

    private Measurement(Guid sensorId, Guid roomId, MetricType metric, double value, DateTimeOffset measuredAt, DateTimeOffset receivedAt)
    {
        SensorId = sensorId;
        RoomId = roomId;
        Metric = metric;
        Value = value;
        MeasuredAt = measuredAt;
        ReceivedAt = receivedAt;
    }

    public Guid SensorId { get; private set; }

    /// <summary>Кімната, де зроблено вимір (історичний факт, не змінюється при перенесенні датчика).</summary>
    public Guid RoomId { get; private set; }

    public MetricType Metric { get; private set; }

    /// <summary>Значення в одиницях метрики (див. <see cref="MetricLimits.Unit"/>).</summary>
    public double Value { get; private set; }

    /// <summary>Час виміру на датчику, UTC.</summary>
    public DateTimeOffset MeasuredAt { get; private set; }

    /// <summary>Час прийому сервером, UTC.</summary>
    public DateTimeOffset ReceivedAt { get; private set; }

    public static Measurement Record(Sensor sensor, MetricType metric, double value, DateTimeOffset measuredAt, DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(sensor);
        Guard.Defined(metric, "Метрика");
        sensor.EnsureCanReport(metric);
        Guard.Finite(value, "Значення виміру");

        var limits = MetricLimits.For(metric);
        if (!limits.Contains(value))
        {
            throw new DomainException(
                $"Значення {value} {MetricLimits.Unit(metric)} поза фізичними межами [{limits.Min}; {limits.Max}] для {metric}.");
        }

        var measuredUtc = measuredAt.ToUniversalTime();
        var receivedUtc = receivedAt.ToUniversalTime();
        if (measuredUtc > receivedUtc + AllowedClockSkew)
        {
            throw new DomainException("Час виміру не може бути в майбутньому.");
        }

        return new Measurement(sensor.Id, sensor.RoomId, metric, value, measuredUtc, receivedUtc);
    }
}
