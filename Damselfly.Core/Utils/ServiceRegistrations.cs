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
        services.AddSingleton<ImageClassifier>();
        services.AddSingleton<FaceONNXService>();

        return services;
    }

    public static IServiceCollection AddSingletonBackEndServices(this IServiceCollection services)
    {
        

        services.AddScoped<IDownloadService>(x => x.GetRequiredService<DownloadService>());
        services.AddScoped<IConfigService>(x => x.GetRequiredService<ConfigService>());
        services.AddScoped<IStatusService>(x => x.GetRequiredService<ServerStatusService>());
        services.AddScoped<IPeopleService>(x => x.GetRequiredService<ImageRecognitionService>());
        services.AddScoped<IRescanService>(x => x.GetRequiredService<RescanService>());
        services.AddScoped<ITagSearchService>(x => x.GetRequiredService<MetaDataService>());
        services.AddScoped<IImageCacheService>(x => x.GetRequiredService<ImageCache>());
        services.AddScoped<ITagService>(x => x.GetRequiredService<ExifService>());
        services.AddScoped<IFolderService>(x => x.GetRequiredService<FolderService>());
        services.AddScoped<ICachedDataService>(x => x.GetRequiredService<CachedDataService>());
        services.AddScoped<IWorkService>(x => x.GetRequiredService<WorkService>());
        services.AddScoped<IThemeService>(x => x.GetRequiredService<ThemeService>());
        // services.AddSingleton<ITaskService>(x => x.GetRequiredService<TaskService>());

        services.AddMLServices();

        return services;
    }

    public static IServiceCollection AddHostedBlazorBackEndServices(this IServiceCollection services)
    {
        services.AddSingletonBackEndServices();

        // services.AddSingleton<ServerNotifierService>();
        services.AddScoped<SearchQueryService>();
        services.AddScoped<RescanService>();
        services.AddScoped<FileService>();
        services.AddScoped<ServerStatusService>();
        services.AddScoped<IStatusService>(x => x.GetRequiredService<ServerStatusService>());
        services.AddScoped<IFileService>( x => x.GetRequiredService<FileService>() );

        //services.AddSingleton<DownloadService>();
        services.AddScoped<IDownloadService>(x => x.GetRequiredService<DownloadService>());

        services.AddScoped<BasketService>();
        services.AddScoped<UserTagRecentsService>();
        services.AddScoped<WordpressService>();
        services.AddScoped<SystemSettingsService>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<AlbumService>();
        services.AddScoped<ImageService>();
        services.AddScoped<EmailMailGunService>();
        services.AddScoped<ThumbnailService>();
        services.AddScoped<IndexingService>();
        services.AddScoped<ExifService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<MetaDataService>();
        services.AddScoped<FolderService>();
        services.AddSingleton<ConfigService>();
        services.AddScoped<ObjectDetector>();
        services.AddScoped<FolderWatcherService>();
        services.AddScoped<ThemeService>();
        services.AddScoped<ImageRecognitionService>();
        services.AddScoped<ImageCache>();
        services.AddScoped<WorkService>();
        services.AddScoped<CachedDataService>();
        services.AddScoped<TaskService>();
        services.AddScoped<RescanService>();
        services.AddScoped<ServerNotifierService>();
        services.AddScoped<ServerStatusService>();
        services.AddScoped<DownloadService>();

        // services.AddScoped<IWordpressService>(x => x.GetRequiredService<WordpressService>());
        services.AddScoped<ISystemSettingsService>(x => x.GetRequiredService<SystemSettingsService>());
        services.AddScoped<IRecentTagService>(x => x.GetRequiredService<UserTagRecentsService>());
        services.AddScoped<IUserMgmtService>(x => x.GetRequiredService<UserManagementService>());

        return services;
    }

    //public static IServiceCollection AddBlazorServerScopedServices(this IServiceCollection services)
    //{
    //    services.AddScoped<ServerSearchService>();
    //    services.AddScoped<SearchQueryService>();
    //    services.AddScoped<NavigationService>();
    //    services.AddScoped<BasketService>();
    //    services.AddScoped<UserFolderService>();
    //    services.AddScoped<UserService>();
    //    services.AddScoped<SelectionService>();
    //    services.AddScoped<UserConfigService>();
    //    services.AddScoped<ViewDataService>();
    //    services.AddScoped<UserThemeService>();
    //    services.AddScoped<UserTagRecentsService>();
    //    services.AddScoped<NotificationsService>();
    //    services.AddScoped<ServerUserStatusService>();
    //    services.AddScoped<WordpressService>();
    //    services.AddScoped<SystemSettingsService>();
    //    services.AddScoped<ApplicationStateService>();
    //    services.AddScoped<UserManagementService>();


    //    services.AddScoped<IRecentTagService>(x => x.GetRequiredService<UserTagRecentsService>());
    //    services.AddScoped<IUserFolderService>(x => x.GetRequiredService<UserFolderService>());
    //    services.AddScoped<IUserService>(x => x.GetRequiredService<UserService>());
    //    services.AddScoped<ISearchService>(x => x.GetRequiredService<ServerSearchService>());
    //    services.AddScoped<IBasketService>(x => x.GetRequiredService<BasketService>());
    //    services.AddScoped<IUserStatusService>(x => x.GetRequiredService<ServerUserStatusService>());
    //    services.AddScoped<IUserMgmtService>(x => x.GetRequiredService<UserManagementService>());

    //    return services;
    //}
}