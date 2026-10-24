using System.Globalization;
using Microclimate.Contracts.Alerts;
using Microclimate.Contracts.Common;

namespace Microclimate.Client.Core.Presentation;

public sealed record AlertItem(AlertResponse Alert, bool CanAcknowledge)
{
    public string Message => Alert.Message;
    public string Details => $"{(Alert.Level == AlertLevel.Critical ? "Критично" : "Попередження")} · {MetricPresentation.MetricName(Alert.Metric)} · {Alert.Value.ToString("0.#", CultureInfo.InvariantCulture)} {MetricPresentation.MetricUnit(Alert.Metric)} · {Alert.RaisedAt.ToLocalTime():dd.MM HH:mm}";
    public string Status => Alert.Status switch { AlertStatus.Open => "Відкрите", AlertStatus.Acknowledged => "Підтверджене", AlertStatus.Resolved => "Вирішене", _ => "Невідомий стан" };
}
