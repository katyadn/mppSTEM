namespace Microclimate.Contracts.Common;

/// <summary>
/// Статус життєвого циклу сповіщення.
/// </summary>
public enum AlertStatus
{
    /// <summary>Активне відкрите сповіщення.</summary>
    Open = 1,

    /// <summary>Сповіщення підтверджене (квітоване) персоналом.</summary>
    Acknowledged = 2,

    /// <summary>Сповіщення вирішене (показники повернулися в норму).</summary>
    Resolved = 3,
}
