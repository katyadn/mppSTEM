namespace Microclimate.Contracts.Measurements;

/// <summary>
/// Результат імпорту файлу вимірювань (CSV).
/// </summary>
/// <param name="TotalRows">Загальна кількість оброблених рядків у файлі.</param>
/// <param name="ImportedCount">Кількість успішно імпортованих вимірів.</param>
/// <param name="SkippedCount">Кількість пропущених рядків (дублікати або поза межами).</param>
/// <param name="Errors">Список текстових описів помилок, якщо такі виникли.</param>
public sealed record ImportResultResponse(
    int TotalRows,
    int ImportedCount,
    int SkippedCount,
    IReadOnlyList<string> Errors);
