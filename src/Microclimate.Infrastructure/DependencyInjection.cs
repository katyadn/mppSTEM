using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Infrastructure.Persistence;
using Microclimate.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microclimate.Infrastructure;

/// <summary>Реєстрація БД і репозиторіїв. Провайдер задається в конфігурації Database:Provider.</summary>
public static class DependencyInjection
{
    public const string DefaultConnectionString = "Data Source=microclimate.db";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = configuration["Database:ConnectionString"] ?? DefaultConnectionString;

        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AppDbContext, SqliteAppDbContext>(o => o.UseSqlite(connectionString));
        }
        else if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AppDbContext, PostgresAppDbContext>(o => o.UseNpgsql(connectionString));
        }
        else
        {
            throw new InvalidOperationException($"Невідомий провайдер БД '{provider}'. Допустимі значення: Sqlite, Postgres.");
        }

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<ISensorRepository, SensorRepository>();
        services.AddScoped<IMeasurementRepository, MeasurementRepository>();
        services.AddScoped<IThresholdRepository, ThresholdRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
