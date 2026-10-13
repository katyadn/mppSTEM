namespace Microclimate.Domain.Enums;

/// <summary>Стан сповіщення. Дозволені переходи: Open → Acknowledged → Resolved, Open → Resolved.</summary>
public enum AlertStatus
{
    Open = 1,
    Acknowledged = 2,
    Resolved = 3,
}
