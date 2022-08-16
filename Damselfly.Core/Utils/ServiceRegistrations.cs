using System;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Damselfly.ML.Face.Accord;
using Damselfly.ML.Face.Azure;
using Damselfly.ML.Face.Emgu;
using Damselfly.ML.ImageClassification;
using Damselfly.ML.ObjectDetection;
using Microsoft.Extensions.DependencyInjection;

namespace Damselfly.Core.Utils;

public static class ServiceRegistrations
{
    public static IServiceCollection AddMLServices(this IServiceCollection services)
    {
        services.AddSingleton(new TransThrottle(CloudTransaction.TransactionType.AzureFace));
        services.AddSingleton<ITransactionThrottle>(x => x.GetRequiredService<TransThrottle>());

        services.AddSingleton<AccordFaceService>();
        services.AddSingleton<AzureFaceService>();
        services.AddSingleton<ImageClassifier>();
        services.AddSingleton<EmguFaceService>();

        return services;
    }

    public static IServiceCollection AddBlazorServerBackEndServices(this IServiceCollection services)
    {
        services.AddSingleton<ConfigService>();
        services.AddSingleton<IConfigService>(x => x.GetRequiredService<ConfigService>());

        services.AddSingleton<StatusService>();
        services.AddSingleton<IStatusService>(x => x.GetRequiredService<StatusService>());

        services.AddSingleton<ObjectDetector>();
        services.AddSingleton<FolderWatcherService>();
        services.AddSingleton<IndexingService>();
        services.AddSingleton<MetaDataService>();
        services.AddSingleton<ThumbnailService>();
        services.AddSingleton<ExifService>();
        services.AddSingleton<TaskService>();
        services.AddSingleton<FolderService>();
        services.AddSingleton<DownloadService>();
        services.AddSingleton<WordpressService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<ImageRecognitionService>();
        services.AddSingleton<ImageCache>();
        services.AddSingleton<WorkService>();
        services.AddSingleton<IWorkService>(x => x.GetRequiredService<WorkService>());

        services.AddSingleton<CachedDataService>();
        services.AddSingleton<ICachedDataService>(x => x.GetRequiredService<CachedDataService>());

        return services;
    }

    public static IServiceCollection AddHostedBlazorBackEndServices( this IServiceCollection services )
    {
        services.AddBlazorServerBackEndServices();

        services.AddSingleton<SearchQueryService>();

        services.AddScoped<BasketService>();
        services.AddScoped<IBasketService>(x => x.GetRequiredService<BasketService>());

        services.AddScoped<UserStatusService>();

        return services;
    }

    public static IServiceCollection AddBlazorServerUIServices( this IServiceCollection services )
	{
        services.AddScoped<ServerSearchService>();
        services.AddScoped<SearchQueryService>();
        services.AddScoped<NavigationService>();
        services.AddScoped<BasketService>();
        services.AddScoped<UserFolderService>();
        services.AddScoped<UserService>();
        services.AddScoped<UserStatusService>();
        services.AddScoped<SelectionService>();
        services.AddScoped<UserConfigService>();
        services.AddScoped<ViewDataService>();
        services.AddScoped<UserThemeService>();
        services.AddScoped<UserTagFavouritesService>();

        services.AddScoped<ISearchService>(x => x.GetRequiredService<ServerSearchService>());
        services.AddScoped<IStatusService>(x => x.GetRequiredService<UserStatusService>());
        services.AddScoped<IBasketService>(x => x.GetRequiredService<BasketService>());

        return services;
    }
}

