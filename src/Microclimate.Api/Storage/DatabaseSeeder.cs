using Microclimate.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Microclimate.Api.Storage;

/// <summary>
/// Застосовує міграції під час старту.
/// Демо-дані додаються лише в порожню БД у середовищі розробки,
/// бо паролі демо-користувачів відомі.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        bool seedDemoData,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        if (!seedDemoData || await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var demo = new InMemoryDataStore(
            scope.ServiceProvider.GetRequiredService<TimeProvider>());
        db.Users.AddRange(demo.Users);
        db.Rooms.AddRange(demo.Rooms);
        db.Sensors.AddRange(demo.Sensors);
        await db.SaveChangesAsync(cancellationToken);
    }
}
