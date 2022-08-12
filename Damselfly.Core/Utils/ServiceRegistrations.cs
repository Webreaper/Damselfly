using System;
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
    public static IServiceCollection AddBackEndServices(this IServiceCollection services)
    {
        services.AddSingleton<StatusService>();
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
        services.AddSingleton<AccordFaceService>();
        services.AddSingleton<AzureFaceService>();
        services.AddSingleton<ImageClassifier>();
        services.AddSingleton<EmguFaceService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<ImageRecognitionService>();
        services.AddSingleton<ImageCache>();
        services.AddSingleton<WorkService>();
        return services;
    }

    public static IServiceCollection AddBlazorServerUIServices( this IServiceCollection services )
	{
        services.AddScoped<SearchService>();
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

        services.AddScoped<IStatusService>(x => x.GetRequiredService<UserStatusService>());
        services.AddScoped<IBasketService>(x => x.GetRequiredService<BasketService>());

        return services;
    }
}

