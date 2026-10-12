using Microclimate.Application.Measurements;
using Microclimate.Domain.Measurements;

namespace Microclimate.Application.Abstractions.Files;

/// <summary>Помилка в рядку CSV-файлу.</summary>
public sealed record CsvLineError(int LineNumber, string Message);

/// <summary>Рядок CSV-файлу імпорту: sensorId,metric,value,measuredAt.</summary>
public sealed record CsvMeasurementRow(int LineNumber, Guid SensorId, MeasurementInput Measurement);

/// <summary>Результат розбору CSV: коректні рядки та помилки.</summary>
public sealed record CsvReadResult(IReadOnlyList<CsvMeasurementRow> Rows, IReadOnlyList<CsvLineError> Errors);

/// <summary>Формат експорту вимірів.</summary>
public enum ExportFormat
{
    Csv = 1,
    Json = 2,
}

/// <summary>Розбір CSV-файлу вимірів (реалізація — Infrastructure).</summary>
public interface IMeasurementCsvReader
{
    Task<CsvReadResult> ReadAsync(Stream content, CancellationToken cancellationToken);
}

/// <summary>Запис вимірів у CSV/JSON потоково (реалізація — Infrastructure).</summary>
public interface IMeasurementExporter
{
    Task ExportAsync(IAsyncEnumerable<Measurement> measurements, Stream destination, ExportFormat format, CancellationToken cancellationToken);
}
