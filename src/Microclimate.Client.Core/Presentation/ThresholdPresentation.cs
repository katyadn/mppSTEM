using Microclimate.Contracts.Common;
using Microclimate.Contracts.Thresholds;

namespace Microclimate.Client.Core.Presentation;

public sealed record ChartLineSection(ChartPoint Start, ChartPoint End, AlertLevel? Level);

public static class ThresholdPresentation
{
    public static AlertLevel? Level(double value, ThresholdResponse? thresholds)
    {
        if (thresholds is null)
            return null;
        if (value < thresholds.CriticalMin || value > thresholds.CriticalMax)
            return AlertLevel.Critical;
        if (value < thresholds.WarningMin || value > thresholds.WarningMax)
            return AlertLevel.Warning;
        return null;
    }

    // Розрізає лінію саме в точках перетину меж, зберігаючи напрямок часу.
    public static IReadOnlyList<ChartLineSection> SplitLine(
        ChartPoint start,
        ChartPoint end,
        ThresholdResponse? thresholds
    )
    {
        var fractions = new List<double> { 0, 1 };
        if (thresholds is not null && start.Value != end.Value)
        {
            var bounds = new[]
            {
                thresholds.CriticalMin,
                thresholds.WarningMin,
                thresholds.WarningMax,
                thresholds.CriticalMax,
            };
            foreach (var bound in bounds.OfType<double>().Distinct())
            {
                var fraction = (bound - start.Value) / (end.Value - start.Value);
                if (fraction > 0 && fraction < 1)
                    fractions.Add(fraction);
            }
        }
        fractions.Sort();
        ChartPoint At(double fraction) =>
            new(
                start.Time.AddTicks((long)((end.Time - start.Time).Ticks * fraction)),
                start.Value + (end.Value - start.Value) * fraction
            );
        return fractions
            .Zip(fractions.Skip(1), (from, to) =>
                new ChartLineSection(At(from), At(to), Level(At((from + to) / 2).Value, thresholds))
            )
            .ToArray();
    }
}
