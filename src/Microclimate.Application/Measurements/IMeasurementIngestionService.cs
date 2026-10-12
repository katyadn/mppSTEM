using Microclimate.Application.Abstractions.Files;
using Microclimate.Domain.Enums;

namespace Microclimate.Application.Measurements;

/// <summary>Один вимір у пакеті від датчика.</summary>
public sealed record MeasurementInput(MetricType Metric, double Value, DateTimeOffset MeasuredAt);

/// <summary>Підсумок обробки пакета вимірів.</summary>
/// <param name="Accepted">Збережено нових вимірів.</param>
/// <param name="Duplicates">Пропущено як уже наявні (повторна відправка).</param>
/// <param name="AlertsRaised">Створено сповіщень.</param>
/// <param name="AlertsResolved">Закрито сповіщень.</param>
public sealed record IngestionResult(int Accepted, int Duplicates, int AlertsRaised, int AlertsResolved);

/// <summary>Підсумок імпорту CSV: або всі рядки збережено, або жоден (тоді Errors не порожній).</summary>
public sealed record ImportResult(int Imported, int Duplicates, IReadOnlyList<CsvLineError> Errors);

/// <summary>Прийом вимірів: валідація, ідемпотентне збереження, генерація сповіщень (docs/analytics.md, розділ 5).</summary>
public interface IMeasurementIngestionService
{
    /// <summary>Максимальна кількість вимірів у пакеті.</summary>
    const int MaxBatchSize = 500;

    /// <summary>
    /// Обробляє пакет автентифікованого датчика в одній транзакції. Некоректний вимір відхиляє весь пакет
    /// (ValidationException), повтори вже збережених вимірів пропускаються.
    /// </summary>
    Task<IngestionResult> IngestAsync(Guid sensorId, IReadOnlyCollection<MeasurementInput> items, CancellationToken cancellationToken);

    /// <summary>Імпорт CSV-файлу вимірів кімнати: усі рядки або жоден.</summary>
    Task<ImportResult> ImportCsvAsync(Guid roomId, Stream content, CancellationToken cancellationToken);
}
