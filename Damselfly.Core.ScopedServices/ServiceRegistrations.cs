using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using static Damselfly.Core.DbModels.AuthorisationRegistration;

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
        services.AddSingleton<NotificationsService>();

        services.AddSingleton<ViewDataService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<UserFolderService>();
        services.AddSingleton<UserService>();

        services.AddSingleton<ClientStatusService>();
        services.AddSingleton<ClientDataService>();
        services.AddSingleton<ClientBasketService>();
        services.AddSingleton<ClientDownloadService>();
        services.AddSingleton<ClientThemeService>();
        services.AddSingleton<ClientRescanService>();
        services.AddSingleton<ClientWorkService>();
        services.AddSingleton<ClientSearchService>();
        services.AddSingleton<ClientConfigService>();
        services.AddSingleton<ClientFolderService>();
        services.AddSingleton<ClientImageCacheService>();
        services.AddSingleton<ClientTagService>();
        services.AddSingleton<ClientTaskService>();
        services.AddSingleton<ClientWordpressService>();
        services.AddSingleton<ClientPeopleService>();
        services.AddSingleton<ApplicationStateService>();
        services.AddSingleton<ClientUserMgmtService>();
        services.AddSingleton<ClientExportService>();

        services.AddSingleton<IPeopleService>(x => x.GetRequiredService<ClientPeopleService>());
        services.AddSingleton<IWordpressService>(x => x.GetRequiredService<ClientWordpressService>());
        services.AddSingleton<IRescanService>(x => x.GetRequiredService<ClientRescanService>());
        services.AddSingleton<IThemeService>(x => x.GetRequiredService<ClientThemeService>());
        services.AddSingleton<IUserFolderService>(x => x.GetRequiredService<UserFolderService>());
        services.AddSingleton<ITagService>(x => x.GetRequiredService<ClientTagService>());
        services.AddSingleton<ITagSearchService>(x => x.GetRequiredService<ClientTagService>());
        services.AddSingleton<ITaskService>(x => x.GetRequiredService<ClientTaskService>());
        services.AddSingleton<IRecentTagService>(x => x.GetRequiredService<ClientTagService>());
        services.AddSingleton<ISearchService>(x => x.GetRequiredService<ClientSearchService>());
        services.AddSingleton<IWorkService>(x => x.GetRequiredService<ClientWorkService>());
        services.AddSingleton<IImageCacheService>(x => x.GetRequiredService<ClientImageCacheService>());
        services.AddSingleton<ICachedDataService>(x => x.GetRequiredService<ClientDataService>());
        services.AddSingleton<IConfigService>(x => x.GetRequiredService<ClientConfigService>());
        services.AddSingleton<IUserConfigService>(x => x.GetRequiredService<ClientConfigService>());
        services.AddSingleton<ISystemSettingsService>(x => x.GetRequiredService<ClientConfigService>());
        services.AddSingleton<IUserService>(x => x.GetRequiredService<UserService>());
        services.AddSingleton<IFolderService>(x => x.GetRequiredService<ClientFolderService>());
        services.AddSingleton<IUserStatusService>(x => x.GetRequiredService<ClientStatusService>());
        services.AddSingleton<IUserBasketService>(x => x.GetRequiredService<ClientBasketService>());
        services.AddSingleton<IBasketService>(x => x.GetRequiredService<ClientBasketService>());
        services.AddSingleton<IDownloadService>(x => x.GetRequiredService<ClientDownloadService>());
        services.AddSingleton<IUserMgmtService>(x => x.GetRequiredService<ClientUserMgmtService>());

        services.AddSingleton<SelectionService>();

        return services;
    }
}