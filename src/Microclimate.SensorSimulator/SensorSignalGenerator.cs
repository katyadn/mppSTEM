using Microclimate.Contracts.Common;
using Microclimate.Contracts.Measurements;
using Microclimate.Domain.Metrics;

using DomainMetricType = Microclimate.Domain.Enums.MetricType;

namespace Microclimate.SensorSimulator;

/// <summary>
/// Генерує пакет вимірів одного датчика. Має стан (генератор випадкових чисел і хід сценарію),
/// тому на кожен датчик створюється окремий екземпляр.
/// </summary>
public sealed class SensorSignalGenerator
{
    private readonly IReadOnlyList<MetricSignal> _signals;
    private readonly Random _random;
    private readonly OverheatScenario? _overheat;

    public SensorSignalGenerator(SimulationScenario scenario, int seed, OverheatOptions overheat, IReadOnlyList<MetricSignal>? signals = null)
    {
        ArgumentNullException.ThrowIfNull(overheat);
        _signals = signals ?? MetricSignal.Defaults;
        _random = new Random(seed);
        _overheat = scenario == SimulationScenario.Overheat ? new OverheatScenario(overheat) : null;
    }

    public IReadOnlyList<MeasurementItem> Next(DateTimeOffset now)
    {
        var hourOfDay = now.TimeOfDay.TotalHours;
        return _signals
            .Select(signal => new MeasurementItem(signal.Metric, Generate(signal, hourOfDay), now))
            .ToList();
    }

    private double Generate(MetricSignal signal, double hourOfDay)
    {
        var value = signal.ValueAt(hourOfDay) + (signal.Noise * ((_random.NextDouble() * 2) - 1));
        if (_overheat is not null && signal.Metric == MetricType.Temperature)
        {
            value = _overheat.Apply(value);
        }

        var range = MetricLimits.For(Enum.Parse<DomainMetricType>(signal.Metric.ToString()));
        return Math.Round(Math.Clamp(value, range.Min, range.Max), signal.Decimals, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Перегрів: температура росте на крок за вимір до піку, тримається на ньому задану кількість вимірів,
    /// далі повертається звичайне значення.
    /// </summary>
    private sealed class OverheatScenario(OverheatOptions options)
    {
        private double? _current;
        private int _held;

        public double Apply(double normal)
        {
            _current ??= normal;
            if (_current < options.PeakTemperature)
            {
                _current = Math.Min(_current.Value + options.StepPerMeasurement, options.PeakTemperature);
                return _current.Value;
            }

            if (_held < options.HoldMeasurements)
            {
                _held++;
                return _current.Value;
            }

            return normal;
        }
    }
}
