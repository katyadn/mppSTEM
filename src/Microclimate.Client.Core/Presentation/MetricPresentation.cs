using Microclimate.Contracts.Common;

namespace Microclimate.Client.Core.Presentation;

// Зіставляє серверні типи метрик з українськими назвами та одиницями.
public static class MetricPresentation
{
    // Повертає українську назву метрики або її текстове значення для невідомого типу.
    public static string MetricName(MetricType metric) => metric switch
    {
        MetricType.Temperature => "Температура",
        MetricType.Humidity => "Вологість",
        MetricType.Co2 => "CO₂",
        MetricType.Pm25 => "PM2.5",
        MetricType.Noise => "Шум",
        MetricType.Light => "Освітленість",
        _ => "Невідома метрика",
    };

    // Повертає одиницю вимірювання метрики або порожній рядок для невідомого типу.
    public static string MetricUnit(MetricType metric) => metric switch
    {
        MetricType.Temperature => "°C",
        MetricType.Humidity => "%",
        MetricType.Co2 => "ppm",
        MetricType.Pm25 => "µg/m³",
        MetricType.Noise => "dB(A)",
        MetricType.Light => "lux",
        _ => string.Empty,
    };
}
