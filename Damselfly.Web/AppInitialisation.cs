using System;
using System.Collections.Generic;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Damselfly.Core.Utils;
using Damselfly.Core.Constants;
using Damselfly.Shared.Utils;
using Microsoft.AspNetCore.Hosting;

namespace Damselfly.Web;

public static class AppInitialiser
{
    /// <summary>
    ///     Called by the Blazor runtime - this is where we setup the HTTP request pipeline and
    ///     initialise all the bits and pieces we need to run.
    /// </summary>
    /// <param name="env"></param>
    public static void SetupServices(this IWebHostEnvironment env, IServiceProvider services)
    {
    }


    /// <summary>
    ///     Bootstrap the task scheduler - configuring all the background scheduled tasks
    ///     that we'll want to run periodically, such as indexing, thumbnail generation,
    ///     cleanup of temporary download files, etc., etc.
    /// </summary>
    private static void StartTaskScheduler(TaskService taskScheduler, DownloadService download,
        ThumbnailService thumbService, ExifService exifService)
    {
        var tasks = new List<ScheduledTask>();

        // Clean up old/irrelevant thumbnails once a week
        var thumbCleanupFreq = new TimeSpan(7, 0, 0, 0);
        tasks.Add(new ScheduledTask
        {
            Type = TaskType.CleanupThumbs,
            ExecutionFrequency = thumbCleanupFreq,
            WorkMethod = () => thumbService.CleanUpThumbnails(thumbCleanupFreq),
            ImmediateStart = false
        });


        // Clean up old download zips from the wwwroot folder
        var downloadCleanupFreq = new TimeSpan(6, 0, 0);
        tasks.Add(new ScheduledTask
        {
            Type = TaskType.CleanupDownloads,
            ExecutionFrequency = downloadCleanupFreq,
            WorkMethod = () => download.CleanUpOldDownloads(downloadCleanupFreq),
            ImmediateStart = false
        });

        // Purge keyword operation entries that have been processed
        var keywordCleanupFreq = new TimeSpan(24, 0, 0);
        tasks.Add(new ScheduledTask
        {
            Type = TaskType.CleanupKeywordOps,
            ExecutionFrequency = new TimeSpan(12, 0, 0),
            WorkMethod = () => { _ = exifService.CleanUpKeywordOperations(keywordCleanupFreq); },
            ImmediateStart = false
        });

        // Dump performance stats out to the logfile
        tasks.Add(new ScheduledTask
        {
            Type = TaskType.DumpPerformance,
            ExecutionFrequency = new TimeSpan(24, 0, 0),
            WorkMethod = () =>
            {
                Action<string> logFunc = Logging.Verbose ? s => Logging.LogVerbose(s) : s => Logging.Log(s);
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
        foreach ( var task in tasks ) taskScheduler.AddTaskDefinition(task);

        // Start the scheduler
        taskScheduler.Start();
    }
}