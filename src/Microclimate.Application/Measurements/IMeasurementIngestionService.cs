using Microclimate.Domain.Enums;

namespace Microclimate.Application.Measurements;

/// <summary>Один вимір у пакеті від датчика.</summary>
public sealed record MeasurementInput(MetricType Metric, double Value, DateTimeOffset MeasuredAt);

/// <summary>Підсумок обробки пакета вимірів.</summary>
/// <param name="Accepted">Збережено вимірів.</param>
/// <param name="AlertsRaised">Створено сповіщень.</param>
/// <param name="AlertsResolved">Закрито сповіщень.</param>
public sealed record IngestionResult(int Accepted, int AlertsRaised, int AlertsResolved);

/// <summary>Прийом вимірів: перевірка, збереження, створення й закриття сповіщень (docs/analytics.md, розділ 4).</summary>
public interface IMeasurementIngestionService
{
    const int MaxBatchSize = 500;

    /// <summary>
    /// Обробляє пакет автентифікованого датчика в одній транзакції.
    /// Некоректний вимір відхиляє весь пакет (ValidationException).
    /// </summary>
    Task<IngestionResult> IngestAsync(Guid sensorId, IReadOnlyCollection<MeasurementInput> items, CancellationToken cancellationToken);
}
