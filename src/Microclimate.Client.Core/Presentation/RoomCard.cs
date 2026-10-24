using System.Globalization;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;

namespace Microclimate.Client.Core.Presentation;

// Клієнт працює з кімнатами; поля будівлі зі старого DTO не використовуються.
// Дані картки кімнати без залежності інтерфейсу від застарілих полів будівлі.
public sealed record RoomCard(Guid Id, string Name, int Floor, string? Type, string StatusText, string MetricsText)
{
    // Створює картку з доступних вимірів і серверного статусу, ігноруючи legacy-поля будівлі.
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
                $"{MetricPresentation.MetricName(metric.Metric)}: {metric.Value.ToString("0.#", CultureInfo.InvariantCulture)} {MetricPresentation.MetricUnit(metric.Metric)}")));
    }

}
