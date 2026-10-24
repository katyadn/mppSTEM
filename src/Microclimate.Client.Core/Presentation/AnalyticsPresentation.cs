using System.Globalization;
using Microclimate.Contracts.Analytics;

namespace Microclimate.Client.Core.Presentation;

// Виміряна точка графіка; відсутні виміри не підміняються нулем.
public sealed record ChartPoint(DateTimeOffset Time, double Value);

// Перетворює серверну аналітику на тексти й неперервні сегменти графіка.
public static class AnalyticsPresentation
{
    // Порожні кошики зберігають розрив, навіть якщо сервер помилково надіслав Avg.
    // Сортує точки за часом; порожній інтервал або некоректне середнє розриває лінію, виміряний нуль зберігається.
    public static IReadOnlyList<IReadOnlyList<ChartPoint>> SplitSeries(IEnumerable<SeriesPointResponse> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var segments = new List<IReadOnlyList<ChartPoint>>();
        List<ChartPoint>? current = null;
        foreach (var point in points.OrderBy(point => point.BucketStart))
        {
            if (point.Count <= 0 || point.Avg is not { } value || !double.IsFinite(value))
            {
                current = null;
                continue;
            }

            if (current is null)
            {
                current = [];
                segments.Add(current);
            }

            current.Add(new ChartPoint(point.BucketStart.ToUniversalTime(), value));
        }

        return segments;
    }

    // Форматує індекс і серверну категорію; NoData або некоректний індекс не перетворює на нуль.
    public static string ComfortText(ComfortIndexResponse? comfort)
    {
        if (comfort is null || comfort.Category == ComfortCategory.NoData ||
            comfort.Index is not { } index || !double.IsFinite(index))
        {
            return "Немає даних";
        }

        // Категорію обчислює сервер; клієнт не дублює пороги домену.
        var category = comfort.Category switch
        {
            ComfortCategory.Comfortable => "Комфортно",
            ComfortCategory.Acceptable => "Прийнятно",
            ComfortCategory.Uncomfortable => "Некомфортно",
            _ => "Невідома категорія",
        };
        return $"{index.ToString("0.#", CultureInfo.InvariantCulture)}/100 · {category}";
    }
}
