using Damselfly.Core.Interfaces;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Damselfly.ML.FaceONNX;
using Damselfly.ML.ImageClassification;
using Damselfly.ML.ObjectDetection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using static Damselfly.Core.DbModels.AuthorisationRegistration;

namespace Damselfly.Core.Utils;

public static class ServiceRegistrations
{
    public static IServiceCollection AddMLServices(this IServiceCollection services)
    {
        services.AddSingleton<ObjectDetector>();
        services.AddSingleton<FaceONNXService>();

        return services;
    }

    public static IServiceCollection AddSingletonBackEndServices(this IServiceCollection services)
    {
        services.AddSingleton<StatisticsService>();
        services.AddSingleton<ConfigService>();
        services.AddSingleton<FolderWatcherService>();
        services.AddSingleton<IndexingService>();
        services.AddSingleton<MetaDataService>();
        services.AddSingleton<ThumbnailService>();
        services.AddSingleton<ExifService>();
        services.AddSingleton<FolderService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<ImageRecognitionService>();
        services.AddSingleton<ImageCache>();
        services.AddSingleton<WorkService>();
        services.AddSingleton<CachedDataService>();
        services.AddSingleton<TaskService>();
        services.AddSingleton<RescanService>();
        services.AddSingleton<ServerNotifierService>();
        services.AddSingleton<ServerStatusService>();
        services.AddSingleton<DownloadService>();

        services.AddSingleton<IDownloadService>(x => x.GetRequiredService<DownloadService>());
        services.AddSingleton<IConfigService>(x => x.GetRequiredService<ConfigService>());
        services.AddSingleton<IStatusService>(x => x.GetRequiredService<ServerStatusService>());
        services.AddSingleton<IPeopleService>(x => x.GetRequiredService<ImageRecognitionService>());
        services.AddSingleton<IRescanService>(x => x.GetRequiredService<RescanService>());
        services.AddSingleton<ITagSearchService>(x => x.GetRequiredService<MetaDataService>());
        services.AddSingleton<IImageCacheService>(x => x.GetRequiredService<ImageCache>());
        services.AddSingleton<ITagService>(x => x.GetRequiredService<ExifService>());
        services.AddSingleton<IFolderService>(x => x.GetRequiredService<FolderService>());
        services.AddSingleton<ICachedDataService>(x => x.GetRequiredService<CachedDataService>());
        services.AddSingleton<IWorkService>(x => x.GetRequiredService<WorkService>());
        services.AddSingleton<IThemeService>(x => x.GetRequiredService<ThemeService>());
        services.AddSingleton<ITaskService>(x => x.GetRequiredService<TaskService>());

        services.AddMLServices();

        return services;
    }

    public static IServiceCollection AddHostedBlazorBackEndServices(this IServiceCollection services)
    {
        services.AddSingletonBackEndServices();

        services.AddSingleton<ServerNotifierService>();
        services.AddSingleton<SearchQueryService>();
        services.AddSingleton<RescanService>();
        services.AddSingleton<FolderService>();
        services.AddSingleton<FileService>();
        services.AddSingleton<ServerStatusService>();
        services.AddSingleton<IStatusService>(x => x.GetRequiredService<ServerStatusService>());
        services.AddSingleton<IFileService>( x => x.GetRequiredService<FileService>() );

        services.AddSingleton<DownloadService>();
        services.AddSingleton<IDownloadService>(x => x.GetRequiredService<DownloadService>());

        services.AddScoped<BasketService>();
        services.AddScoped<UserTagRecentsService>();
        services.AddScoped<WordpressService>();
        services.AddScoped<SystemSettingsService>();
        services.AddScoped<UserManagementService>();

        services.AddScoped<IWordpressService>(x => x.GetRequiredService<WordpressService>());
        services.AddScoped<ISystemSettingsService>(x => x.GetRequiredService<SystemSettingsService>());
        services.AddScoped<IRecentTagService>(x => x.GetRequiredService<UserTagRecentsService>());
        services.AddScoped<IUserMgmtService>(x => x.GetRequiredService<UserManagementService>());

        return services;
    }

    public static IServiceCollection AddBlazorServerScopedServices(this IServiceCollection services)
    {
        services.AddScoped<ServerSearchService>();
        services.AddScoped<SearchQueryService>();
        services.AddScoped<NavigationService>();
        services.AddScoped<BasketService>();
        services.AddScoped<UserFolderService>();
        services.AddScoped<UserService>();
        services.AddScoped<SelectionService>();
        services.AddScoped<UserConfigService>();
        services.AddScoped<ViewDataService>();
        services.AddScoped<UserThemeService>();
        services.AddScoped<UserTagRecentsService>();
        services.AddScoped<NotificationsService>();
        services.AddScoped<ServerUserStatusService>();
        services.AddScoped<WordpressService>();
        services.AddScoped<SystemSettingsService>();
        services.AddScoped<ApplicationStateService>();
        services.AddScoped<UserManagementService>();

        services.AddScoped<IRecentTagService>(x => x.GetRequiredService<UserTagRecentsService>());
        services.AddScoped<IUserFolderService>(x => x.GetRequiredService<UserFolderService>());
        services.AddScoped<IUserService>(x => x.GetRequiredService<UserService>());
        services.AddScoped<ISearchService>(x => x.GetRequiredService<ServerSearchService>());
        services.AddScoped<IBasketService>(x => x.GetRequiredService<BasketService>());
        services.AddScoped<IUserStatusService>(x => x.GetRequiredService<ServerUserStatusService>());
        services.AddScoped<IUserMgmtService>(x => x.GetRequiredService<UserManagementService>());

        return services;
    }
}