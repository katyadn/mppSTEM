using Microclimate.Domain.Common;

namespace Microclimate.Domain.Sensors;

/// <summary>Датчик мікроклімату, встановлений у кімнаті. Один датчик може надсилати будь-які метрики.</summary>
public sealed class Sensor : Entity<Guid>
{
    public const int SerialNumberMaxLength = 64;
    public const int ModelMaxLength = 100;
    public const int DeviceKeyHashMaxLength = 256;

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

    public Guid RoomId { get; private set; }

    public string SerialNumber { get; private set; }

    public string? Model { get; private set; }

    /// <summary>Хеш ключа X-Device-Key; сам ключ не зберігається.</summary>
    public string DeviceKeyHash { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset InstalledAt { get; private set; }

    public DateTimeOffset? LastSeenAt { get; private set; }

    public static Sensor Create(Guid roomId, string serialNumber, string? model, string deviceKeyHash, DateTimeOffset now) =>
        new(
            Guid.CreateVersion7(now),
            Guard.NotEmpty(roomId, "Кімната"),
            Guard.RequiredText(serialNumber, "Серійний номер", SerialNumberMaxLength),
            Guard.OptionalText(model, "Модель", ModelMaxLength),
            Guard.RequiredText(deviceKeyHash, "Хеш ключа датчика", DeviceKeyHashMaxLength),
            now.ToUniversalTime());

    public void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException($"Датчик {SerialNumber} деактивовано.");
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

    public void Deactivate() => IsActive = false;

    /// <summary>Ротація секретного ключа датчика.</summary>
    public void RotateDeviceKey(string newDeviceKeyHash)
    {
        EnsureActive();
        DeviceKeyHash = Guard.RequiredText(newDeviceKeyHash, "Хеш ключа датчика", DeviceKeyHashMaxLength);
    }
}
