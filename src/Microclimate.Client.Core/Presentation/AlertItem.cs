using System.Globalization;
using Microclimate.Contracts.Alerts;
using Microclimate.Contracts.Common;

namespace Microclimate.Client.Core.Presentation;

// Подання сповіщення для інтерфейсу з локалізованими текстами та дозволом на підтвердження.
public sealed record AlertItem(AlertResponse Alert, bool CanAcknowledge)
{
    // Назва кімнати та українська назва метрики сповіщення.
    public string Message => Alert.Message;
    // Рівень, виміряне значення з одиницею та місцевий час сповіщення.
    public string Details => $"{(Alert.Level == AlertLevel.Critical ? "Критично" : "Попередження")} · {MetricPresentation.MetricName(Alert.Metric)} · {Alert.Value.ToString("0.#", CultureInfo.InvariantCulture)} {MetricPresentation.MetricUnit(Alert.Metric)} · {Alert.RaisedAt.ToLocalTime():dd.MM HH:mm}";
    // Український підпис поточного стану сповіщення.
    public string Status => Alert.Status switch { AlertStatus.Open => "Відкрите", AlertStatus.Acknowledged => "Підтверджене", AlertStatus.Resolved => "Вирішене", _ => "Невідомий стан" };
}
