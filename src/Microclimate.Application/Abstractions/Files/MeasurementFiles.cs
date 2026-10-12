using Microclimate.Domain.Measurements;

namespace Microclimate.Application.Abstractions.Files;

/// <summary>Формат експорту вимірів.</summary>
public enum ExportFormat
{
    Csv = 1,
    Json = 2,
}

/// <summary>Запис вимірів у CSV або JSON потоково (реалізація — Infrastructure).</summary>
public interface IMeasurementExporter
{
    Task ExportAsync(IAsyncEnumerable<Measurement> measurements, Stream destination, ExportFormat format, CancellationToken cancellationToken);
}
