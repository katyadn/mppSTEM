using Microclimate.Application.Analytics;
using Microclimate.Domain.Enums;

namespace Microclimate.Api.Services;

/// <summary>
/// Обчислення індексу комфорту (docs/analytics.md, розділ 3).
/// </summary>
public sealed class ComfortIndexCalculator : IComfortIndexCalculator
{
    public ComfortIndexResult Calculate(IReadOnlyCollection<ComfortInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        if (inputs.Count == 0)
        {
            return new ComfortIndexResult(null, ComfortCategory.NoData, []);
        }

        var scores = new List<MetricComfortScore>();
        var hasCritical = false;
        double weightedSum = 0;
        double totalWeight = 0;

        foreach (var input in inputs)
        {
            var weight = ComfortIndexSettings.Weight(input.Metric);
            if (weight <= 0)
            {
                continue;
            }

            var score = CalculateMetricScore(input.Value, input.Limits);
            if (score <= 0)
            {
                hasCritical = true;
            }

            scores.Add(new MetricComfortScore(input.Metric, input.Value, score, weight));
            weightedSum += score * weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            return new ComfortIndexResult(null, ComfortCategory.NoData, scores);
        }

        var ci = Math.Round(weightedSum / totalWeight, 2);

        var category = hasCritical
            ? ComfortCategory.Uncomfortable
            : ci switch
            {
                >= ComfortIndexSettings.ComfortableFrom => ComfortCategory.Comfortable,
                >= ComfortIndexSettings.AcceptableFrom => ComfortCategory.Acceptable,
                _ => ComfortCategory.Uncomfortable,
            };

        return new ComfortIndexResult(ci, category, scores);
    }

    private static double CalculateMetricScore(double v, Domain.Thresholds.ThresholdLimits limits)
    {
        if (limits.CriticalMin.HasValue && v < limits.CriticalMin.Value)
        {
            return 0;
        }

        if (limits.CriticalMax.HasValue && v > limits.CriticalMax.Value)
        {
            return 0;
        }

        if (limits.WarningMin.HasValue && v < limits.WarningMin.Value)
        {
            var warningMin = limits.WarningMin.Value;
            var d = warningMin - v;
            var dMax = limits.CriticalMin.HasValue
                ? warningMin - limits.CriticalMin.Value
                : warningMin * 0.20;

            if (dMax <= 0)
            {
                return 0;
            }

            return Math.Max(0, Math.Min(100, Math.Round(100 * (1 - d / dMax), 2)));
        }

        if (limits.WarningMax.HasValue && v > limits.WarningMax.Value)
        {
            var warningMax = limits.WarningMax.Value;
            var d = v - warningMax;
            var dMax = limits.CriticalMax.HasValue
                ? limits.CriticalMax.Value - warningMax
                : warningMax * 0.20;

            if (dMax <= 0)
            {
                return 0;
            }

            return Math.Max(0, Math.Min(100, Math.Round(100 * (1 - d / dMax), 2)));
        }

        return 100;
    }
}
