using Microclimate.Domain.Thresholds;

namespace Microclimate.Application.Analytics;

/// <summary>Індекс комфорту за формулою з docs/analytics.md, розділ 3.</summary>
public sealed class ComfortIndexCalculator : IComfortIndexCalculator
{
    private static readonly ComfortIndexResult NoData = new(null, ComfortCategory.NoData, []);

    public ComfortIndexResult Calculate(IReadOnlyCollection<ComfortInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var components = inputs
            .Select(i => new MetricComfortScore(i.Metric, i.Value, Score(i.Value, i.Limits), ComfortIndexSettings.Weight(i.Metric)))
            .Where(c => c.Weight > 0)
            .ToList();

        var totalWeight = components.Sum(c => c.Weight);
        if (totalWeight == 0)
        {
            return NoData;
        }

        var index = Math.Round(components.Sum(c => c.Weight * c.Score) / totalWeight, 2);
        return new ComfortIndexResult(index, Categorize(index, components), components);
    }

    /// <summary>100 у межах Warning, лінійно до 0 на межі Critical, 0 за нею.</summary>
    public static double Score(double value, ThresholdLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        if (limits.WarningMax is { } warningMax && value > warningMax)
        {
            var criticalMax = limits.CriticalMax ?? warningMax * (1 + ComfortIndexSettings.MissingCriticalSpanRatio);
            return Linear(value - warningMax, criticalMax - warningMax);
        }

        if (limits.WarningMin is { } warningMin && value < warningMin)
        {
            var criticalMin = limits.CriticalMin ?? warningMin * (1 - ComfortIndexSettings.MissingCriticalSpanRatio);
            return Linear(warningMin - value, warningMin - criticalMin);
        }

        // Межі Warning відсутні з цього боку, але є критична межа.
        return value > limits.CriticalMax || value < limits.CriticalMin ? 0 : 100;
    }

    private static double Linear(double distance, double span) =>
        span <= 0 ? 0 : Math.Max(0, 100 * (1 - (distance / span)));

    private static ComfortCategory Categorize(double index, IReadOnlyCollection<MetricComfortScore> components)
    {
        if (components.Any(c => c.Score == 0) || index < ComfortIndexSettings.AcceptableFrom)
        {
            return ComfortCategory.Uncomfortable;
        }

        return index >= ComfortIndexSettings.ComfortableFrom ? ComfortCategory.Comfortable : ComfortCategory.Acceptable;
    }
}
