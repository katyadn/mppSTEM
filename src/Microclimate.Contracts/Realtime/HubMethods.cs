namespace Microclimate.Contracts.Realtime;

/// <summary>
/// Константи назв методів та подій хаба SignalR для клієнта і сервера.
/// </summary>
public static class HubMethods
{
    /// <summary>Подія фіксації нового перевищення порогу (тривоги).</summary>
    public const string AlertRaised = "AlertRaised";

    /// <summary>Подія квитування тривоги оператором.</summary>
    public const string AlertAcknowledged = "AlertAcknowledged";

    /// <summary>Подія надходження свіжих показників вимірювань для кімнати.</summary>
    public const string MeasurementReceived = "MeasurementReceived";
}
