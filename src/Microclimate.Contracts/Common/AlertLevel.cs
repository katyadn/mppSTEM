namespace Microclimate.Contracts.Common;

/// <summary>
/// Рівень критичності сповіщення про стан мікроклімату.
/// </summary>
public enum AlertLevel
{
    /// <summary>Попереджувальний рівень відхилення показника.</summary>
    Warning = 1,

    /// <summary>Критичний рівень відхилення показника.</summary>
    Critical = 2,
}
