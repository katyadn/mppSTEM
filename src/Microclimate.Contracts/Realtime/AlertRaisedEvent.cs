using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Realtime;

/// <summary>
/// Подія реального часу: виявлено перевищення порогу мікроклімату.
/// </summary>
/// <param name="AlertId">Унікальний ідентифікатор створеного сповіщення.</param>
/// <param name="RoomId">Ідентифікатор кімнати.</param>
/// <param name="RoomName">Назва кімнати для швидкого відображення в тості/банері.</param>
/// <param name="Metric">Тип метрики.</param>
/// <param name="Level">Рівень небезпеки (Warning / Critical).</param>
/// <param name="Value">Зафіксоване значення.</param>
/// <param name="Threshold">Поріг, який було порушено.</param>
/// <param name="Message">Текстове повідомлення.</param>
/// <param name="CreatedAt">Час виникнення (UTC).</param>
public sealed record AlertRaisedEvent(
    Guid AlertId,
    Guid RoomId,
    string RoomName,
    MetricType Metric,
    AlertLevel Level,
    double Value,
    double Threshold,
    string Message,
    DateTimeOffset CreatedAt);
