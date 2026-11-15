using Microclimate.Contracts.Common;

namespace Microclimate.SensorSimulator;

/// <summary>
/// Параметри добового коливання метрики та випадкового шуму.
/// Значення найбільше о годині PeakHour.
/// </summary>
public sealed record MetricSignal(MetricType Metric, double Base,
    double Amplitude, double PeakHour, double Noise, int Decimals)
{
    private const double HoursPerDay = 24;

    public static IReadOnlyList<MetricSignal> Defaults { get; } =
    [
        new(MetricType.Temperature, Base: 22.0, Amplitude: 1.5,
            PeakHour: 15, Noise: 0.2, Decimals: 1),
        new(MetricType.Humidity, Base: 45, Amplitude: 5, PeakHour: 5,
            Noise: 1, Decimals: 0),
        new(MetricType.Co2, Base: 700, Amplitude: 250, PeakHour: 13,
            Noise: 30, Decimals: 0),
        new(MetricType.Pm25, Base: 6, Amplitude: 2, PeakHour: 19, Noise: 1,
            Decimals: 0),
    ];

    /// <summary>Значення без шуму для моменту доби <paramref
    /// name="hourOfDay"/>.</summary>
    public double ValueAt(double hourOfDay) =>
        Base + (Amplitude * Math.Cos(2 * Math.PI * (
            hourOfDay - PeakHour) / HoursPerDay));
}
