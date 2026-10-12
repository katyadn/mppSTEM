namespace Microclimate.Domain.Enums;

/// <summary>Рівень сповіщення про перевищення порогу.</summary>
public enum AlertLevel
{
    /// <summary>Значення поза межами норми, але в межах критичних.</summary>
    Warning = 1,

    /// <summary>Значення за критичною межею.</summary>
    Critical = 2,
}
