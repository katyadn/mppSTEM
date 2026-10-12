using Microsoft.Extensions.DependencyInjection;

namespace Microclimate.Application;

/// <summary>Реєстрація сервісів шару Application.</summary>
public static class DependencyInjection
{
    /// <summary>Реєстрації з'являться разом із реалізаціями сервісів на Етапах 1–2.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
