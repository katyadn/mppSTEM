using Microclimate.Domain.Enums;

namespace Microclimate.Domain.Sensors;

/// <summary>Метрика, яку вміє вимірювати датчик (рядок таблиці SensorCapabilities).</summary>
public sealed class SensorCapability
{
    // Для EF Core
    private SensorCapability()
    {
    }

    internal SensorCapability(Guid sensorId, MetricType metric)
    {
        SensorId = sensorId;
        Metric = metric;
    }

    public Guid SensorId { get; private set; }

    public MetricType Metric { get; private set; }
}
