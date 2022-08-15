using System;
using Damselfly.Core.Interfaces;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Damselfly.Core.ScopedServices;

public static class ServiceRegistrations
{
    public static IServiceCollection AddDamselflyUIServices( this IServiceCollection services )
    {
        services.AddScoped<ViewDataService>();
        services.AddScoped<CachedDataService>();
        services.AddScoped<APIBasketService>();
        services.AddScoped<APIDownloadService>();
        services.AddScoped<ClientThemeService>();
        services.AddScoped<ClientUserService>();
        services.AddScoped<ClientWordpressService>();
        services.AddScoped<NavigationService>();
        services.AddScoped<SearchService>();
        services.AddScoped<APIConfigService>();
        services.AddScoped<StatusService>();
        services.AddScoped<UserStatusService>();
        services.AddScoped<APIFolderService>();

        services.AddScoped<IConfigService>(x => x.GetRequiredService<APIConfigService>());
        services.AddScoped<IFolderService>(x => x.GetRequiredService<APIFolderService>());
        services.AddScoped<IStatusService>(x => x.GetRequiredService<UserStatusService>());
        services.AddScoped<IBasketService>(x => x.GetRequiredService<APIBasketService>());
        services.AddScoped<IDownloadService>(x => x.GetRequiredService<APIDownloadService>());

        services.AddScoped<SelectionService>();

        return services;
    }
}

