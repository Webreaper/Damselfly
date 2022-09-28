using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Damselfly.Core.ScopedServices;

public static class ServiceRegistrations
{
    /// <summary>
    ///     Set up UI Services for Blazor WASM
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddDamselflyUIServices(this IServiceCollection services)
    {
        services.AddScoped<NotificationsService>();
        services.AddScoped<ViewDataService>();
        services.AddScoped<NavigationService>();
        services.AddScoped<WebAssemblyStatusService>();
        services.AddScoped<ClientExportService>();
        services.AddScoped<SelectionService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserStatusService, ClientStatusService>();
        services.AddScoped<ICachedDataService, ClientDataService>();
        services.AddScoped<IDownloadService, ClientDownloadService>();
        services.AddScoped<IThemeService, ClientThemeService>();
        services.AddScoped<IRescanService, ClientRescanService>();
        services.AddScoped<IWorkService, ClientWorkService>();
        services.AddScoped<ISearchService, ClientSearchService>();
        services.AddScoped<IImageCacheService, ClientImageCacheService>();
        services.AddScoped<ITaskService, ClientTaskService>();
        services.AddScoped<IWordpressService, ClientWordpressService>();
        services.AddScoped<IPeopleService, ClientPeopleService>();
        services.AddScoped<IUserMgmtService, ClientUserMgmtService>();
        services.AddScoped<IFolderService, ClientFolderService>();
        services.AddScoped<IUserFolderService, UserFolderService>();

        services.AddScoped<ClientBasketService>();
        services.AddScoped<IUserBasketService>(x => x.GetRequiredService<ClientBasketService>());
        services.AddScoped<IBasketService>(x => x.GetRequiredService<ClientBasketService>());

        services.AddScoped<ClientConfigService>();
        services.AddScoped<IConfigService>(x => x.GetRequiredService<ClientConfigService>());
        services.AddScoped<IUserConfigService>(x => x.GetRequiredService<ClientConfigService>());
        services.AddScoped<ISystemSettingsService>(x => x.GetRequiredService<ClientConfigService>());

        services.AddScoped<ClientTagService>();
        services.AddScoped<ITagService>(x => x.GetRequiredService<ClientTagService>());
        services.AddScoped<ITagSearchService>(x => x.GetRequiredService<ClientTagService>());
        services.AddScoped<IRecentTagService>(x => x.GetRequiredService<ClientTagService>());
 
        return services;
    }
}