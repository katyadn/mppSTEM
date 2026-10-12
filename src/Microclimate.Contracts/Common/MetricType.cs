namespace Microclimate.Contracts.Common;

/// <summary>
/// Тип вимірюваної фізичної метрики мікроклімату.
/// </summary>
public enum MetricType
{
    /// <summary>Температура повітря (°C).</summary>
    Temperature = 1,

    /// <summary>Відносна вологість (%).</summary>
    Humidity = 2,

    /// <summary>Концентрація діоксиду вуглецю (ppm).</summary>
    Co2 = 3,

    /// <summary>Дрібнодисперсний пил PM2.5 (µg/m³).</summary>
    Pm25 = 4,

    /// <summary>Рівень акустичного шуму (dB).</summary>
    Noise = 5,

    /// <summary>Рівень освітленості (lux).</summary>
    Light = 6,
}
