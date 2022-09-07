using System;
using Damselfly.Core.Interfaces;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Damselfly.Core.ScopedServices;

public static class ServiceRegistrations
{
    /// <summary>
    /// Set up UI Services for Blazor WASM
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddDamselflyUIServices(this IServiceCollection services)
    {
        services.AddScoped<NotificationsService>();

        services.AddScoped<ViewDataService>();
        services.AddScoped<NavigationService>();
        services.AddScoped<UserFolderService>();
        services.AddScoped<UserService>();

        services.AddScoped<ClientStatusService>();
        services.AddScoped<ClientDataService>();
        services.AddScoped<ClientBasketService>();
        services.AddScoped<ClientDownloadService>();
        services.AddScoped<ClientThemeService>();
        services.AddScoped<ClientRescanService>();
        services.AddScoped<ClientWorkService>();
        services.AddScoped<ClientSearchService>();
        services.AddScoped<ClientConfigService>();
        services.AddScoped<ClientFolderService>();
        services.AddScoped<ClientImageCacheService>();
        services.AddScoped<ClientTagService>();
        services.AddScoped<ClientTaskService>();
        services.AddScoped<ClientWordpressService>();
        services.AddScoped<ClientPeopleService>();
        services.AddScoped<WebAssemblyStatusService>();
        services.AddScoped<ClientUserMgmtService>();

        services.AddScoped<IPeopleService>(x => x.GetRequiredService<ClientPeopleService>());
        services.AddScoped<IWordpressService>(x => x.GetRequiredService<ClientWordpressService>());
        services.AddScoped<IRescanService>(x => x.GetRequiredService<ClientRescanService>());
        services.AddScoped<IThemeService>(x => x.GetRequiredService<ClientThemeService>());
        services.AddScoped<IUserFolderService>(x => x.GetRequiredService<UserFolderService>());
        services.AddScoped<ITagService>(x => x.GetRequiredService<ClientTagService>());
        services.AddScoped<ITagSearchService>(x => x.GetRequiredService<ClientTagService>());
        services.AddScoped<ITaskService>(x => x.GetRequiredService<ClientTaskService>());
        services.AddScoped<IRecentTagService>(x => x.GetRequiredService<ClientTagService>());
        services.AddScoped<ISearchService>(x => x.GetRequiredService<ClientSearchService>());
        services.AddScoped<IWorkService>(x => x.GetRequiredService<ClientWorkService>());
        services.AddScoped<IImageCacheService>(x => x.GetRequiredService<ClientImageCacheService>());
        services.AddScoped<ICachedDataService>(x => x.GetRequiredService<ClientDataService>());
        services.AddScoped<IConfigService>(x => x.GetRequiredService<ClientConfigService>());
        services.AddScoped<ISystemSettingsService>(x => x.GetRequiredService<ClientConfigService>());
        services.AddScoped<IUserService>(x => x.GetRequiredService<UserService>());
        services.AddScoped<IFolderService>(x => x.GetRequiredService<ClientFolderService>());
        services.AddScoped<IUserStatusService>(x => x.GetRequiredService<ClientStatusService>());
        services.AddScoped<IUserBasketService>(x => x.GetRequiredService<ClientBasketService>());
        services.AddScoped<IBasketService>(x => x.GetRequiredService<ClientBasketService>());
        services.AddScoped<IDownloadService>(x => x.GetRequiredService<ClientDownloadService>());
        services.AddScoped<IUserMgmtService>(x => x.GetRequiredService<ClientUserMgmtService>());

        services.AddScoped<SelectionService>();

        return services;
    }
}

