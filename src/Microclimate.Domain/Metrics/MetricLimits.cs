using Microclimate.Domain.Enums;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Domain.Metrics;

/// <summary>
/// Фізично можливі межі значень датчиків. Значення поза межами вважаються помилкою даних,
/// а не поганим мікрокліматом (для цього є пороги).
/// </summary>
public static class MetricLimits
{
    public static MetricRange For(MetricType metric) => MetricCatalog.Get(metric).PhysicalRange;

    public static string Unit(MetricType metric) => MetricCatalog.Get(metric).Unit;
}
