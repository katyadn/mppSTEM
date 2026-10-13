using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;

namespace Microclimate.Domain.Alerts;

/// <summary>Сповіщення про вихід метрики кімнати за поріг.</summary>
public sealed class Alert : Entity<Guid>
{
    public const int MessageMaxLength = 500;

    // Для EF Core
    private Alert()
    {
        Message = string.Empty;
    }

    private Alert(Guid id, Guid roomId, Guid? sensorId, MetricType metric, AlertLevel level, double value, double thresholdValue, string message, DateTimeOffset raisedAt)
    {
        Id = id;
        RoomId = roomId;
        SensorId = sensorId;
        Metric = metric;
        Level = level;
        Value = value;
        ThresholdValue = thresholdValue;
        Message = message;
        RaisedAt = raisedAt;
        Status = AlertStatus.Open;
    }

    public Guid RoomId { get; private set; }

    public Guid? SensorId { get; private set; }

    public MetricType Metric { get; private set; }

    public AlertLevel Level { get; private set; }

    public AlertStatus Status { get; private set; }

    public double Value { get; private set; }

    /// <summary>Межа, яку перевищено (знімок на момент події).</summary>
    public double ThresholdValue { get; private set; }

    public string Message { get; private set; }

    public DateTimeOffset RaisedAt { get; private set; }

    public DateTimeOffset? AcknowledgedAt { get; private set; }

    public Guid? AcknowledgedByUserId { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public static Alert Raise(
        Guid roomId,
        Guid? sensorId,
        MetricType metric,
        AlertLevel level,
        double value,
        double thresholdValue,
        string message,
        DateTimeOffset now)
    {
        if (sensorId == Guid.Empty)
        {
            throw new DomainException("Датчик сповіщення не може мати порожній ідентифікатор.");
        }

        return new Alert(
            Guid.CreateVersion7(now),
            Guard.NotEmpty(roomId, "Кімната"),
            sensorId,
            Guard.Defined(metric, "Метрика"),
            Guard.Defined(level, "Рівень"),
            Guard.Finite(value, "Значення"),
            Guard.Finite(thresholdValue, "Межа порогу"),
            Guard.RequiredText(message, "Повідомлення", MessageMaxLength),
            now.ToUniversalTime());
    }

    /// <summary>Оператор підтверджує, що побачив сповіщення. Можливо лише для відкритого сповіщення.</summary>
    public void Acknowledge(Guid userId, DateTimeOffset now)
    {
        if (Status != AlertStatus.Open)
        {
            throw new DomainException($"Підтвердити можна лише відкрите сповіщення (поточний стан: {Status}).");
        }

        AcknowledgedByUserId = Guard.NotEmpty(userId, "Користувач");
        AcknowledgedAt = NotBeforeRaised(now);
        Status = AlertStatus.Acknowledged;
    }

    public void Resolve(DateTimeOffset now)
    {
        if (Status == AlertStatus.Resolved)
        {
            throw new DomainException("Сповіщення вже закрито.");
        }

        ResolvedAt = NotBeforeRaised(now);
        Status = AlertStatus.Resolved;
    }

    private DateTimeOffset NotBeforeRaised(DateTimeOffset now)
    {
        var utc = now.ToUniversalTime();
        if (utc < RaisedAt)
        {
            throw new DomainException("Час зміни стану не може бути раніше за час створення сповіщення.");
        }

        return utc;
    }
}
