using Microclimate.Domain.Common;

namespace Microclimate.Domain.Buildings;

/// <summary>Будівля, що містить кімнати.</summary>
public sealed class Building : Entity<Guid>
{
    public const int NameMaxLength = 100;
    public const int AddressMaxLength = 250;

    // Для EF Core
    private Building()
    {
        Name = string.Empty;
    }

    private Building(Guid id, string name, string? address, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Address = address;
        CreatedAt = createdAt;
    }

    public string Name { get; private set; }

    public string? Address { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Building Create(string name, string? address, DateTimeOffset now) =>
        new(
            Guid.CreateVersion7(now),
            Guard.RequiredText(name, "Назва будівлі", NameMaxLength),
            Guard.OptionalText(address, "Адреса", AddressMaxLength),
            now.ToUniversalTime());

    public void Update(string name, string? address)
    {
        Name = Guard.RequiredText(name, "Назва будівлі", NameMaxLength);
        Address = Guard.OptionalText(address, "Адреса", AddressMaxLength);
    }
}
