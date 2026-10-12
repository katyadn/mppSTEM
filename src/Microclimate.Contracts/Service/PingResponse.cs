namespace Microclimate.Contracts.Service;

/// <summary>
/// Службова відповідь перевірки доступності та версії API.
/// </summary>
/// <param name="Status">Статус сервісу (наприклад, "ok").</param>
/// <param name="Version">Версія контракту API (наприклад, "1.0").</param>
/// <param name="ServerTime">Поточний серверний час у форматі UTC.</param>
public sealed record PingResponse(
    string Status,
    string Version,
    DateTimeOffset ServerTime);
