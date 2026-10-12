namespace Microclimate.Domain.Enums;

/// <summary>Показник мікроклімату. Одиниці — див. <see cref="Metrics.MetricLimits"/>.</summary>
public enum MetricType
{
    /// <summary>Температура, °C.</summary>
    Temperature = 1,

    /// <summary>Відносна вологість, %.</summary>
    Humidity = 2,

    /// <summary>Концентрація CO₂, ppm.</summary>
    Co2 = 3,

    /// <summary>Дрібнодисперсні частки PM2.5, µg/m³.</summary>
    Pm25 = 4,

    /// <summary>Рівень шуму, dB(A).</summary>
    Noise = 5,

    /// <summary>Освітленість, lux.</summary>
    Light = 6,
}
