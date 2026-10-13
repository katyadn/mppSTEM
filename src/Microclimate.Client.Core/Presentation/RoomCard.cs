using System.Globalization;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;

namespace Microclimate.Client.Core.Presentation;

// Клієнт працює з кімнатами; поля будівлі зі старого DTO не використовуються.
public sealed record RoomCard(Guid Id, string Name, int Floor, string? Type, string StatusText, string MetricsText)
{
    public static RoomCard FromResponse(RoomSummaryResponse room)
    {
        ArgumentNullException.ThrowIfNull(room);
        var status = room.LatestMetrics.Count == 0 ? "Немає даних" : room.Status switch
        {
            "Normal" => "Норма",
            "Warning" => "Попередження",
            "Critical" => "Критично",
            _ => "Невідомий стан",
        };
        return new(room.Id, room.Name, room.Floor, room.Type, status,
            string.Join(" · ", room.LatestMetrics.Select(metric =>
                $"{MetricName(metric.Metric)}: {metric.Value.ToString("0.#", CultureInfo.InvariantCulture)} {MetricUnit(metric.Metric)}")));
    }

    private static string MetricName(MetricType metric) => metric switch
    {
        MetricType.Temperature => "Температура",
        MetricType.Humidity => "Вологість",
        MetricType.Co2 => "CO₂",
        MetricType.Pm25 => "PM2.5",
        MetricType.Noise => "Шум",
        MetricType.Light => "Освітленість",
        _ => "Невідома метрика",
    };

    private static string MetricUnit(MetricType metric) => metric switch
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
