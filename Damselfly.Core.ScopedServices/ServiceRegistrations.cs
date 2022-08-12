using System;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Damselfly.Core.ScopedServices;

public static class ServiceRegistrations
{
    public static IServiceCollection AddDamselflyUIServices( this IServiceCollection services )
    {
        services.AddScoped<CachedDataService>();
        services.AddScoped<APIBasketService>();
        services.AddScoped<NavigationService>();
        services.AddScoped<SearchService>();
        services.AddScoped<APIConfigService>();
        services.AddScoped<StatusService>();
        services.AddScoped<UserStatusService>();
        services.AddScoped<ViewDataService>();

        services.AddScoped<IStatusService>(x => x.GetRequiredService<UserStatusService>());
        services.AddScoped<IBasketService>(x => x.GetRequiredService<APIBasketService>());

        services.AddScoped<SelectionService>();

        return services;
    }
}

