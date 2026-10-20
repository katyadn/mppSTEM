using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Domain.Alerts;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Measurements;
using Microclimate.Domain.Rooms;
using Microclimate.Domain.Sensors;
using Microclimate.Domain.Thresholds;
using Microclimate.Domain.Users;

using Microsoft.EntityFrameworkCore;

namespace Microclimate.Infrastructure.Persistence;

/// <summary>
/// Спільний контекст БД. Для кожного провайдера є свій нащадок (SqliteAppDbContext, PostgresAppDbContext)
/// з окремим набором міграцій (docs/db-schema.md, розділ 6).
/// </summary>
public abstract class AppDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Sensor> Sensors => Set<Sensor>();

    public DbSet<Measurement> Measurements => Set<Measurement>();

    public DbSet<Threshold> Thresholds => Set<Threshold>();

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<User> Users => Set<User>();

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        var strategy = Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        // Таблиці описано в Configurations/* за docs/db-schema.md, розділ 3.
        // Id сутностей створює домен (Guid v7), тому в конфігураціях стоїть ValueGeneratedNever.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // Enum-и зберігаються рядками (DECISIONS.md, D-1-003).
        configurationBuilder.Properties<MetricType>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<RoomType>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<AlertLevel>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<AlertStatus>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<UserRole>().HaveConversion<string>().HaveMaxLength(16);
    }
}
