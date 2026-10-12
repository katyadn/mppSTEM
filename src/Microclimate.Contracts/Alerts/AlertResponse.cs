using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Alerts;

/// <summary>
/// Інформація про сповіщення щодо перевищення порогу мікроклімату.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор сповіщення.</param>
/// <param name="RoomId">Ідентифікатор кімнати, де зафіксовано порушення.</param>
/// <param name="SensorId">Ідентифікатор датчика (якщо прив'язано до конкретного пристрою).</param>
/// <param name="Metric">Тип фізичної метрики.</param>
/// <param name="Level">Рівень критичності (Warning або Critical).</param>
/// <param name="Status">Статус сповіщення (Open, Acknowledged, Resolved).</param>
/// <param name="Value">Значення, що перевищило поріг, в одиницях метрики.</param>
/// <param name="ThresholdValue">Значення граничного порогу на момент події.</param>
/// <param name="Message">Текстове повідомлення про інцидент.</param>
/// <param name="RaisedAt">Час виникнення сповіщення (UTC).</param>
/// <param name="AcknowledgedAt">Час квитування оператором (UTC).</param>
/// <param name="AcknowledgedByUserId">Ідентифікатор користувача, який квитував сповіщення.</param>
/// <param name="ResolvedAt">Час повернення показників до норми (UTC).</param>
public sealed record AlertResponse(
    Guid Id,
    Guid RoomId,
    Guid? SensorId,
    MetricType Metric,
    AlertLevel Level,
    AlertStatus Status,
    double Value,
    double ThresholdValue,
    string Message,
    DateTimeOffset RaisedAt,
    DateTimeOffset? AcknowledgedAt,
    Guid? AcknowledgedByUserId,
    DateTimeOffset? ResolvedAt);
