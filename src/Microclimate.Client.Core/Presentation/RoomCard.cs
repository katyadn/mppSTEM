using System.Globalization;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;

namespace Microclimate.Client.Core.Presentation;

// Дані картки кімнати без залежності інтерфейсу від застарілих полів будівлі.
public sealed record RoomCard(Guid Id, string Name, int Floor, string? Type, string StatusText, string MetricsText)
{
    public string StatusCode { get; init; } = "NoData";
    public string FloorText => $"Поверх {Floor}";
    public string TemperatureText { get; init; } = "—";
    public string HumidityText { get; init; } = "—";
    public string Co2Text { get; init; } = "—";
    public string Pm25Text { get; init; } = "—";
    public string NoiseText { get; init; } = "—";
    public string LightText { get; init; } = "—";

    public string UpdatedText { get; init; } = "Вимірів ще немає";

    // Створює картку з доступних вимірів і серверного статусу, ігноруючи legacy-поля будівлі.
    public static RoomCard FromResponse(RoomSummaryResponse room)
    {
        ArgumentNullException.ThrowIfNull(room);
        var status =
            room.LatestMetrics.Count == 0
                ? "Немає даних"
                : room.Status switch
                {
                    "Comfortable" => "Комфортно",
                    "Acceptable" => "Прийнятно",
                    "Uncomfortable" => "Некомфортно",
                    "NoData" => "Немає актуальної оцінки комфорту",
                    "Normal" => "Норма",
                    "Warning" => "Попередження",
                    "Critical" => "Критично",
                    _ => "Невідомий стан",
                };
        return new(
            room.Id,
            room.Name,
            room.Floor,
            room.Type,
            status,
            string.Join(
                " · ",
                room.LatestMetrics.Select(metric =>
                    $"{MetricPresentation.MetricName(metric.Metric)}: {metric.Value.ToString("0.#", CultureInfo.InvariantCulture)} {MetricPresentation.MetricUnit(metric.Metric)}"
                )
            )
        )
        {
            StatusCode = room.LatestMetrics.Count == 0 ? "NoData" : room.Status,
            TemperatureText = MetricText(room, MetricType.Temperature),
            HumidityText = MetricText(room, MetricType.Humidity),
            Co2Text = MetricText(room, MetricType.Co2),
            Pm25Text = MetricText(room, MetricType.Pm25),
            NoiseText = MetricText(room, MetricType.Noise),
            LightText = MetricText(room, MetricType.Light),
            UpdatedText =
                room.LatestMetrics.Count == 0
                    ? "Вимірів ще немає"
                    : $"Останній вимір: {room.LatestMetrics.Max(metric => metric.MeasuredAt).ToLocalTime():dd.MM HH:mm}",
        };
    }

    private static string MetricText(RoomSummaryResponse room, MetricType type)
    {
        var latest = room.LatestMetrics.Where(metric => metric.Metric == type).MaxBy(metric => metric.MeasuredAt);
        return latest is null
            ? "—"
            : $"{latest.Value.ToString("0.#", CultureInfo.InvariantCulture)} {MetricPresentation.MetricUnit(type)}";
    }
}
