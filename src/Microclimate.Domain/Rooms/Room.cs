using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;

namespace Microclimate.Domain.Rooms;

/// <summary>Приміщення, в якому встановлено датчики.</summary>
public sealed class Room : Entity<Guid>
{
    public const int NameMaxLength = 100;
    public const int MinFloor = -5;
    public const int MaxFloor = 200;

    // Для EF Core
    private Room()
    {
        Name = string.Empty;
    }

    private Room(Guid id, DateTimeOffset createdAt)
    {
        Id = id;
        CreatedAt = createdAt;
        Name = string.Empty;
    }

    public string Name { get; private set; }

    public int Floor { get; private set; }

    public decimal? AreaM2 { get; private set; }

    public RoomType Type { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Room Create(string name, int floor, decimal? areaM2, RoomType type, DateTimeOffset now)
    {
        var room = new Room(Guid.CreateVersion7(now), now.ToUniversalTime());
        room.Update(name, floor, areaM2, type);
        return room;
    }

    public void Update(string name, int floor, decimal? areaM2, RoomType type)
    {
        if (floor is < MinFloor or > MaxFloor)
        {
            throw new DomainException($"Поверх має бути в межах від {MinFloor} до {MaxFloor}.");
        }

        if (areaM2 <= 0)
        {
            throw new DomainException("Площа кімнати має бути додатною.");
        }

        Name = Guard.RequiredText(name, "Назва кімнати", NameMaxLength);
        Floor = floor;
        AreaM2 = areaM2;
        Type = Guard.Defined(type, "Тип кімнати");
    }
}
