namespace Microclimate.Domain.Enums;

/// <summary>Стан сповіщення. Дозволені переходи: Open → Acknowledged → Resolved, Open → Resolved.</summary>
public enum AlertStatus
{
    /// <summary>Сповіщення активне й ще не опрацьоване.</summary>
    Open = 1,

    /// <summary>Оператор побачив сповіщення.</summary>
    Acknowledged = 2,

    /// <summary>Значення повернулося в норму.</summary>
    Resolved = 3,
}
