using System.Collections.ObjectModel;

using Microsoft.Extensions.Options;

namespace Microclimate.SensorSimulator;

/// <summary>Налаштування симулятора, секція "Simulator" (docs/simulator.md, розділ 4).</summary>
public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    public static readonly TimeSpan MinPeriod = TimeSpan.FromSeconds(1);

    public Uri? ApiBaseUrl { get; set; }

    /// <summary>Як часто кожен датчик надсилає пакет вимірів.</summary>
    public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Початкове значення генератора; датчик i отримує Seed + i.</summary>
    public int Seed { get; set; } = 42;

    public Collection<SimulatedSensorOptions> Sensors { get; } = [];

    public OverheatOptions Overheat { get; set; } = new();
}

/// <summary>Один датчик, від імені якого працює симулятор.</summary>
public sealed class SimulatedSensorOptions
{
    public Guid? SensorId { get; set; }

    /// <summary>Ключ X-Device-Key. Задається через user-secrets або змінні середовища, не в git.</summary>
    public string DeviceKey { get; set; } = string.Empty;

    public SimulationScenario Scenario { get; set; } = SimulationScenario.Normal;
}

/// <summary>Параметри сценарію перегріву.</summary>
public sealed class OverheatOptions
{
    /// <summary>Температура, до якої піднімається значення, °C.</summary>
    public double PeakTemperature { get; set; } = 29;

    /// <summary>Приріст температури за один вимір, °C.</summary>
    public double StepPerMeasurement { get; set; } = 0.1;

    /// <summary>Скільки вимірів температура тримається на піку.</summary>
    public int HoldMeasurements { get; set; } = 30;
}

public enum SimulationScenario
{
    Normal = 1,
    Overheat = 2,
}

/// <summary>Перевірка налаштувань під час старту, щоб помилка конфігурації не проявилась пізніше в циклі.</summary>
public sealed class SimulatorOptionsValidator : IValidateOptions<SimulatorOptions>
{
    public ValidateOptionsResult Validate(string? name, SimulatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var errors = new List<string>();

        if (options.ApiBaseUrl is not { IsAbsoluteUri: true })
        {
            errors.Add("Simulator:ApiBaseUrl має бути абсолютною адресою API.");
        }

        if (options.Period < SimulatorOptions.MinPeriod)
        {
            errors.Add($"Simulator:Period має бути не менше {SimulatorOptions.MinPeriod}.");
        }

        if (options.Sensors.Count == 0)
        {
            errors.Add("Simulator:Sensors має містити хоча б один датчик.");
        }

        for (var i = 0; i < options.Sensors.Count; i++)
        {
            var sensor = options.Sensors[i];
            if (sensor.SensorId.GetValueOrDefault() == Guid.Empty)
            {
                errors.Add($"Simulator:Sensors:{i}:SensorId не задано.");
            }

            if (string.IsNullOrWhiteSpace(sensor.DeviceKey))
            {
                errors.Add($"Simulator:Sensors:{i}:DeviceKey не задано.");
            }

            if (!Enum.IsDefined(sensor.Scenario))
            {
                errors.Add($"Simulator:Sensors:{i}:Scenario має бути Normal або Overheat.");
            }
        }

        var overheat = options.Overheat;
        if (overheat.StepPerMeasurement <= 0 || overheat.HoldMeasurements < 0)
        {
            errors.Add("Simulator:Overheat: крок має бути додатним, а тривалість піку — невід'ємною.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
