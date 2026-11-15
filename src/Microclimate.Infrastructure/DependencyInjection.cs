using Microclimate.Application.Abstractions.Files;
using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Infrastructure.Files;
using Microclimate.Infrastructure.Persistence;
using Microclimate.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microclimate.Infrastructure;

/// <summary>Реєстрація БД, репозиторіїв і експорту. Провайдер і рядок підключення обов'язково задаються в конфігурації.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = Required(configuration, DatabaseSettings.ProviderKey);
        var connectionString = Required(configuration, DatabaseSettings.ConnectionStringKey);

        if (string.Equals(provider, DatabaseSettings.Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AppDbContext, SqliteAppDbContext>(o => o.UseSqlite(connectionString));
        }
        else if (string.Equals(provider, DatabaseSettings.Postgres, StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AppDbContext, PostgresAppDbContext>(o => o.UseNpgsql(connectionString));
        }
        else
        {
            throw new InvalidOperationException(
                $"Невідомий провайдер БД '{provider}'. Допустимі значення: {DatabaseSettings.Sqlite}, {DatabaseSettings.Postgres}.");
        }

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<ISensorRepository, SensorRepository>();
        services.AddScoped<IMeasurementRepository, MeasurementRepository>();
        services.AddScoped<IThresholdRepository, ThresholdRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services.AddMeasurementExport();
    }

    /// <summary>Експорт не залежить від БД, тому його можна підключити й без AddInfrastructure.</summary>
    public static IServiceCollection AddMeasurementExport(this IServiceCollection services)
    {
        services.TryAddSingleton<IMeasurementExporter, MeasurementExporter>();
        return services;
    }

    // Без тихих значень за замовчуванням: помилка конфігурації видна одразу під час старту.
    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Не задано налаштування '{key}'.")
            : value;
    }
}
