using System;
using Serilog;
using System.Collections.Generic;
using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.Services;
using Damselfly.ML.Face.Azure;
using Damselfly.ML.ObjectDetection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Damselfly.Core.Utils;
using Damselfly.Shared.Utils;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Web.Client.Shared;

namespace Damselfly.Web;

public static class AppInitialiser
{
    /// <summary>
    /// Called by the Blazor runtime - this is where we setup the HTTP request pipeline and
    /// initialise all the bits and pieces we need to run.
    /// </summary>
    /// <param name="env"></param>
    public static void SetupServices(this IWebHostEnvironment env, IServiceProvider services)
    {
        var download = services.GetRequiredService<DownloadService>();
        var tasks = services.GetRequiredService<TaskService>();
        var thumbService = services.GetRequiredService<ThumbnailService>();
        var exifService = services.GetRequiredService<ExifService>();
        var imageProcService = services.GetRequiredService<ImageProcessService>();

        // Prime the cache
        services.GetRequiredService<ImageCache>().WarmUp().Wait();

        // Start the work processing queue for AI, Thumbs, etc
        services.GetRequiredService<WorkService>().StartService();

        // Start the face service before the thumbnail service
        services.GetRequiredService<AzureFaceService>().StartService().Wait();
        services.GetRequiredService<MetaDataService>().StartService();
        services.GetRequiredService<IndexingService>().StartService();
        services.GetRequiredService<ImageRecognitionService>().StartService();

        // ObjectDetector can throw a segmentation fault if the docker container is pinned
        // to a single CPU, so for now, to aid debugging, let's not even try and initialise
        // it if AI is disabled. See https://github.com/Webreaper/Damselfly/issues/334
        if (!services.GetRequiredService<ConfigService>().GetBool(ConfigSettings.DisableObjectDetector, false))
            services.GetRequiredService<ObjectDetector>().InitScorer();

        // Validation check to ensure at least one user is an Admin
        // WASM: How, when it's scoped?
        // services.GetRequiredService<UserManagementService>().CheckAdminUser().Wait();

        StartTaskScheduler(tasks, download, thumbService, exifService);
    }


    /// <summary>
    /// Bootstrap the task scheduler - configuring all the background scheduled tasks
    /// that we'll want to run periodically, such as indexing, thumbnail generation,
    /// cleanup of temporary download files, etc., etc.
    /// </summary>
    private static void StartTaskScheduler(TaskService taskScheduler, DownloadService download,
                                        ThumbnailService thumbService, ExifService exifService)
    {
        var tasks = new List<ScheduledTask>();

        // Clean up old/irrelevant thumbnails once a week
        var thumbCleanupFreq = new TimeSpan(7, 0, 0, 0);
        tasks.Add(new ScheduledTask
        {
            Type = ScheduledTask.TaskType.CleanupThumbs,
            ExecutionFrequency = thumbCleanupFreq,
            WorkMethod = () => thumbService.CleanUpThumbnails(thumbCleanupFreq),
            ImmediateStart = false
        });


        // Clean up old download zips from the wwwroot folder
        var downloadCleanupFreq = new TimeSpan(6, 0, 0);
        tasks.Add(new ScheduledTask
        {
            Type = ScheduledTask.TaskType.CleanupDownloads,
            ExecutionFrequency = downloadCleanupFreq,
            WorkMethod = () => download.CleanUpOldDownloads(downloadCleanupFreq),
            ImmediateStart = false
        });

        // Purge keyword operation entries that have been processed
        var keywordCleanupFreq = new TimeSpan(24, 0, 0);
        tasks.Add(new ScheduledTask
        {
            Type = ScheduledTask.TaskType.CleanupKeywordOps,
            ExecutionFrequency = new TimeSpan(12, 0, 0),
            WorkMethod = () => { _ = exifService.CleanUpKeywordOperations(keywordCleanupFreq); },
            ImmediateStart = false
        });

        // Dump performance stats out to the logfile
        tasks.Add(new ScheduledTask
        {
            Type = ScheduledTask.TaskType.DumpPerformance,
            ExecutionFrequency = new TimeSpan(24, 0, 0),
            WorkMethod = () =>
            {
                Action<string> logFunc = Logging.Verbose ? (s) => Logging.LogVerbose(s) : (s) => Logging.Log(s);
                Stopwatch.WriteTotals(logFunc);
            },
            ImmediateStart = false
        });

#if false
            // Disabled for now, don't think it's really required.
            // Flush the DB WriteCache (currently a no-op except for SQLite) ever 2 hours
            tasks.Add( new ScheduledTask
            {
                Type = ScheduledTask.TaskType.FlushDBWriteCache,
                ExecutionFrequency = new TimeSpan(2, 0, 0),
                WorkMethod = () =>
                {
                    using var db = new ImageContext();

                    db.FlushDBWriteCache();
                }
            });
#endif

        // Add the jobs
        foreach (var task in tasks)
        {
            taskScheduler.AddTaskDefinition(task);
        }

        // Start the scheduler
        taskScheduler.Start();
    }
}

