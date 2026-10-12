namespace Microclimate.Contracts.Sensors;

/// <summary>
/// Відповідь при реєстрації або ротації ключа датчика, що містить одноразово відкритий ключ.
/// </summary>
/// <param name="Sensor">Дані створеного або оновленого датчика.</param>
/// <param name="DeviceKey">Секретний відкритий ключ пристрою для заголовка X-Device-Key (показується один раз!).</param>
public sealed record SensorCreatedResponse(
    SensorResponse Sensor,
    string DeviceKey);
