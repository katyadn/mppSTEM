using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;

namespace Microclimate.Domain.Sensors;

/// <summary>Датчик мікроклімату, встановлений у кімнаті.</summary>
public sealed class Sensor : Entity<Guid>
{
    public const int SerialNumberMaxLength = 64;
    public const int ModelMaxLength = 100;
    public const int DeviceKeyHashMaxLength = 256;

    private readonly List<SensorCapability> _capabilities = [];

    // Для EF Core
    private Sensor()
    {
        SerialNumber = string.Empty;
        DeviceKeyHash = string.Empty;
    }

    private Sensor(Guid id, Guid roomId, string serialNumber, string? model, string deviceKeyHash, DateTimeOffset installedAt)
    {
        Id = id;
        RoomId = roomId;
        SerialNumber = serialNumber;
        Model = model;
        DeviceKeyHash = deviceKeyHash;
        InstalledAt = installedAt;
        IsActive = true;
    }

    /// <summary>Поточна кімната датчика.</summary>
    public Guid RoomId { get; private set; }

    public string SerialNumber { get; private set; }

    public string? Model { get; private set; }

    /// <summary>Хеш ключа X-Device-Key; сам ключ не зберігається.</summary>
    public string DeviceKeyHash { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset InstalledAt { get; private set; }

    /// <summary>Час останнього прийнятого виміру.</summary>
    public DateTimeOffset? LastSeenAt { get; private set; }

    /// <summary>Метрики, які вимірює датчик.</summary>
    public IReadOnlyCollection<MetricType> Capabilities => _capabilities.Select(c => c.Metric).ToArray();

    public static Sensor Create(
        Guid roomId,
        string serialNumber,
        string? model,
        string deviceKeyHash,
        IEnumerable<MetricType> capabilities,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        var metrics = capabilities.Select(m => Guard.Defined(m, "Метрика датчика")).Distinct().ToArray();
        if (metrics.Length == 0)
        {
            throw new DomainException("Датчик має вимірювати хоча б одну метрику.");
        }

        var sensor = new Sensor(
            Guid.CreateVersion7(now),
            Guard.NotEmpty(roomId, "Кімната"),
            Guard.RequiredText(serialNumber, "Серійний номер", SerialNumberMaxLength),
            Guard.OptionalText(model, "Модель", ModelMaxLength),
            Guard.RequiredText(deviceKeyHash, "Хеш ключа датчика", DeviceKeyHashMaxLength),
            now.ToUniversalTime());

        sensor._capabilities.AddRange(metrics.Select(m => new SensorCapability(sensor.Id, m)));
        return sensor;
    }

    public bool Supports(MetricType metric) => _capabilities.Exists(c => c.Metric == metric);

    /// <summary>Перевіряє, що датчик може надіслати вимір метрики.</summary>
    public void EnsureCanReport(MetricType metric)
    {
        if (!IsActive)
        {
            throw new DomainException($"Датчик {SerialNumber} деактивовано.");
        }

        if (!Supports(metric))
        {
            throw new DomainException($"Датчик {SerialNumber} не вимірює метрику {metric}.");
        }
    }

    /// <summary>Фіксує час останнього виміру; час не зсувається назад.</summary>
    public void MarkSeen(DateTimeOffset moment)
    {
        var utc = moment.ToUniversalTime();
        if (LastSeenAt is null || utc > LastSeenAt)
        {
            LastSeenAt = utc;
        }
    }

    /// <summary>Переносить датчик в іншу кімнату. Попередні виміри залишаються прив'язаними до старої кімнати.</summary>
    public void MoveTo(Guid roomId) => RoomId = Guard.NotEmpty(roomId, "Кімната");

    public void RotateKey(string newDeviceKeyHash) =>
        DeviceKeyHash = Guard.RequiredText(newDeviceKeyHash, "Хеш ключа датчика", DeviceKeyHashMaxLength);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
