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
        services.AddScoped<IPeopleService>(x => x.GetRequiredService<ImageRecognitionService>());
        services.AddScoped<IRescanService>(x => x.GetRequiredService<RescanService>());
        services.AddScoped<ITagSearchService>(x => x.GetRequiredService<MetaDataService>());
        services.AddScoped<IImageCacheService>(x => x.GetRequiredService<ImageCache>());
        services.AddScoped<ITagService>(x => x.GetRequiredService<ExifService>());
        services.AddScoped<ICachedDataService>(x => x.GetRequiredService<CachedDataService>());
        services.AddScoped<IWorkService>(x => x.GetRequiredService<WorkService>());
        services.AddScoped<IThemeService>(x => x.GetRequiredService<ThemeService>());
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<IIpOriginService, IpApiService>();
        // services.AddSingleton<ITaskService>(x => x.GetRequiredService<TaskService>());

        services.AddMLServices();

        return services;
    }

    public static IServiceCollection AddHostedBlazorBackEndServices(this IServiceCollection services)
    {
        services.AddSingletonBackEndServices();

        services.AddScoped<SearchQueryService>();
        services.AddScoped<RescanService>();
        services.AddScoped<FileService>();
        services.AddScoped<IFileService>( x => x.GetRequiredService<FileService>() );

        services.AddScoped<IDownloadService>(x => x.GetRequiredService<DownloadService>());

        services.AddScoped<NotificationService>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<AlbumService>();
        services.AddScoped<ImageService>();
        services.AddScoped<EmailMailGunService>();
        services.AddScoped<ThumbnailService>();
        services.AddScoped<IndexingService>();
        services.AddScoped<ExifService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<MetaDataService>();
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
        services.AddScoped<DownloadService>();
        services.AddScoped<PhotoShootService>();
        services.AddScoped<ProductService>();
        services.AddScoped<PaymentTransactionService>();


        services.AddScoped<IRecentTagService>(x => x.GetRequiredService<UserTagRecentsService>());
        services.AddScoped<IUserMgmtService>(x => x.GetRequiredService<UserManagementService>());

        return services;
    }

}