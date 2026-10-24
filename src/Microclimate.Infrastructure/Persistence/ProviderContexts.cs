using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Microclimate.Infrastructure.Persistence;

/// <summary>Контекст для SQLite: розробка, тести, запуск без Docker.</summary>
public sealed class SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options) : AppDbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // SQLite не вміє порівнювати й сортувати DateTimeOffset; увесь час в UTC, тож число зберігає порядок.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }
}

/// <summary>Контекст для PostgreSQL: основна БД у Docker.</summary>
public sealed class PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options) : AppDbContext(options);

/// <summary>Створення контексту командою dotnet ef migrations add ... --context SqliteAppDbContext.</summary>
public sealed class SqliteDesignTimeFactory : IDesignTimeDbContextFactory<SqliteAppDbContext>
{
    public SqliteAppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SqliteAppDbContext>().UseSqlite(DatabaseSettings.DesignTimeSqliteConnection).Options);
}

/// <summary>
/// Створення контексту командою dotnet ef migrations add ... --context PostgresAppDbContext.
/// Для генерації міграцій підключення до сервера не потрібне.
/// </summary>
public sealed class PostgresDesignTimeFactory : IDesignTimeDbContextFactory<PostgresAppDbContext>
{
    public PostgresAppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PostgresAppDbContext>().UseNpgsql(DatabaseSettings.DesignTimePostgresConnection).Options);
}
