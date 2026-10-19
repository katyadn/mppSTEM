using Microclimate.Application.Analytics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microclimate.Application;

/// <summary>Реєстрація сервісів шару Application.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IComfortIndexCalculator, ComfortIndexCalculator>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }
}
