using Microclimate.Domain.Common;

namespace Microclimate.Domain.ValueObjects;

/// <summary>Замкнений діапазон значень [Min, Max].</summary>
public sealed record MetricRange
{
    public MetricRange(double min, double max)
    {
        Guard.Finite(min, nameof(min));
        Guard.Finite(max, nameof(max));
        if (min > max)
        {
            throw new DomainException("Нижня межа діапазону не може перевищувати верхню.");
        }

        Min = min;
        Max = max;
    }

    public double Min { get; }

    public double Max { get; }

    public bool Contains(double value) => value >= Min && value <= Max;
}
