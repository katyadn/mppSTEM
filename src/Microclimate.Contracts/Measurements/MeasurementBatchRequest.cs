namespace Microclimate.Contracts.Measurements;

/// <summary>
/// Пакет вимірів від одного датчика (до 500 елементів за один HTTP-запит).
/// </summary>
/// <param name="SensorId">Ідентифікатор сенсора, що передає пакет.</param>
/// <param name="Items">Список окремих вимірів.</param>
public sealed record MeasurementBatchRequest(
    Guid SensorId,
    IReadOnlyList<MeasurementItem> Items);
