using Microclimate.Domain.Measurements;

namespace Microclimate.Application.Abstractions.Files;

/// <summary>Формат експорту вимірів.</summary>
public enum ExportFormat
{
    Csv = 1,
    Json = 2,
}

/// <summary>Тип вмісту й розширення файлу для кожного формату експорту.</summary>
public static class ExportFormats
{
    public static string ContentType(ExportFormat format) => format switch
    {
        ExportFormat.Csv => "text/csv",
        ExportFormat.Json => "application/json",
        _ => throw Unknown(format),
    };

    public static string FileExtension(ExportFormat format) => format switch
    {
        ExportFormat.Csv => "csv",
        ExportFormat.Json => "json",
        _ => throw Unknown(format),
    };

    private static ArgumentOutOfRangeException Unknown(ExportFormat format) =>
        new(nameof(format), format, "Невідомий формат експорту.");
}

/// <summary>Запис вимірів у CSV або JSON потоково (реалізація — Infrastructure).</summary>
public interface IMeasurementExporter
{
    Task ExportAsync(IAsyncEnumerable<Measurement> measurements, Stream destination, ExportFormat format, CancellationToken cancellationToken);
}
