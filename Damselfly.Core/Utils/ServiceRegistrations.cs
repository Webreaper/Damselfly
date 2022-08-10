using System;
using Damselfly.Core.ScopedServices;
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
    public static IServiceCollection AddDamselflyServices(this IServiceCollection services)
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

    public static IServiceCollection AddUserServices( this IServiceCollection services )
	{
        return services.AddScoped<UserFolderService>()
                       .AddScoped<UserService>()
                       .AddScoped<UserStatusService>()
                       .AddScoped<UserConfigService>()
                       .AddScoped<ViewDataService>()
                       .AddScoped<UserThemeService>()
                       .AddScoped<UserTagFavouritesService>();

    }
}

