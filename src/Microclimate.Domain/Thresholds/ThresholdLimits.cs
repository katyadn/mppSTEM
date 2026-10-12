using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;

namespace Microclimate.Domain.Thresholds;

/// <summary>
/// Межі порогу метрики: CriticalMin ≤ WarningMin &lt; WarningMax ≤ CriticalMax.
/// Будь-яка межа може бути відсутня, але хоча б одна має бути задана. Межі включні.
/// </summary>
public sealed record ThresholdLimits
{
    public ThresholdLimits(double? criticalMin, double? warningMin, double? warningMax, double? criticalMax)
    {
        if (criticalMin is null && warningMin is null && warningMax is null && criticalMax is null)
        {
            throw new DomainException("Поріг має містити хоча б одну межу.");
        }

        CheckFinite(criticalMin, nameof(criticalMin));
        CheckFinite(warningMin, nameof(warningMin));
        CheckFinite(warningMax, nameof(warningMax));
        CheckFinite(criticalMax, nameof(criticalMax));

        if (criticalMin > warningMin)
        {
            throw new DomainException("Критична нижня межа не може бути вищою за нижню межу попередження.");
        }

        if (warningMax > criticalMax)
        {
            throw new DomainException("Верхня межа попередження не може бути вищою за критичну верхню межу.");
        }

        double?[] lower = [criticalMin, warningMin];
        double?[] upper = [warningMax, criticalMax];
        foreach (var low in lower)
        {
            foreach (var high in upper)
            {
                if (low >= high)
                {
                    throw new DomainException("Нижні межі порогу мають бути строго меншими за верхні.");
                }
            }
        }

        CriticalMin = criticalMin;
        WarningMin = warningMin;
        WarningMax = warningMax;
        CriticalMax = criticalMax;
    }

    public double? CriticalMin { get; }

    public double? WarningMin { get; }

    public double? WarningMax { get; }

    public double? CriticalMax { get; }

    /// <summary>Оцінює значення: <c>null</c> — норма, інакше рівень сповіщення.</summary>
    public AlertLevel? Evaluate(double value)
    {
        if (value < CriticalMin || value > CriticalMax)
        {
            return AlertLevel.Critical;
        }

        if (value < WarningMin || value > WarningMax)
        {
            return AlertLevel.Warning;
        }

        return null;
    }

    /// <summary>Межа, яку перевищено значенням (знімок для сповіщення), або <c>null</c>, якщо значення в нормі.</summary>
    public double? ExceededBound(double value)
    {
        if (value < CriticalMin)
        {
            return CriticalMin;
        }

        if (value > CriticalMax)
        {
            return CriticalMax;
        }

        if (value < WarningMin)
        {
            return WarningMin;
        }

        return value > WarningMax ? WarningMax : null;
    }

    private static void CheckFinite(double? value, string field)
    {
        if (value is { } number)
        {
            Guard.Finite(number, field);
        }
    }
}
