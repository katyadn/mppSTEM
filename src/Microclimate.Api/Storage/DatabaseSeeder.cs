using Microclimate.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Microclimate.Api.Storage;

/// <summary>
/// Підготовка БД під час старту: застосовує міграції, а в порожню БД додає ті самі демо-дані,
/// що й сховище в пам'яті (адміністратор, оператор, кімната, датчик).
/// </summary>
public static class DatabaseSeeder
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var demo = new InMemoryDataStore(scope.ServiceProvider.GetRequiredService<TimeProvider>());
        db.Users.AddRange(demo.Users);
        db.Rooms.AddRange(demo.Rooms);
        db.Sensors.AddRange(demo.Sensors);
        await db.SaveChangesAsync(cancellationToken);
    }
}
