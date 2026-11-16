using Microclimate.Domain.Sensors;

namespace Microclimate.Domain.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid RoomId = Guid.Parse("0199a1b2-0000-7000-8000-000000000001");

    public static readonly Guid UserId = Guid.Parse("0199a1b2-0000-7000-8000-000000000002");

    public static Sensor Sensor() => Domain.Sensors.Sensor.Create(RoomId, "SN-001", "Model X", "hash", Now);
}
