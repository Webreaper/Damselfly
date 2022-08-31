using System;
using System.IO;
using Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Components.Authorization;
using Damselfly.Core.Services;
using Damselfly.Core.ImageProcessing;
using Damselfly.Core.ScopedServices;
using System.Collections.Generic;
using Damselfly.Core.Models;
using Tewr.Blazor.FileReader;
using Radzen;
using Damselfly.Core.Utils;
using Damselfly.Core.Interfaces;
using Damselfly.ML.ObjectDetection;
using Damselfly.ML.Face.Accord;
using Damselfly.ML.Face.Azure;
using Damselfly.ML.Face.Emgu;
using Damselfly.ML.ImageClassification;
using Damselfly.Areas.Identity;
using Damselfly.Core.DbModels;
using MudBlazor.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Damselfly.Core.Constants;
using Microsoft.AspNetCore.DataProtection;
using Syncfusion.Blazor;
using Syncfusion.Licensing;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Web;
using Damselfly.Shared.Utils;

namespace Damselfly.Web
{
    /// <summary>
    /// Core initialisation and management of the startup of the app.
    /// Responsible for bootstrapping all the services, and setting
    /// up the scheduled tasks that we'll want to run.
    /// </summary>
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Called by the Blazor runtime. We set up our services here.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {

        }

        /// <summary>
        /// Called by the Blazor runtime - this is where we setup the HTTP request pipeline and
        /// initialise all the bits and pieces we need to run.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ConfigService configService,
            DownloadService download, TaskService tasks, ThumbnailService thumbService, ExifService exifService,
            ImageCache imageCache, ImageProcessService imageService, AzureFaceService azureService, IndexingService indexingService,
            ImageRecognitionService aiService, ObjectDetector objectDetector, ThemeService themeService, WorkService workService,
            MetaDataService metaDataService, UserManagementService userManagement)
        {
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
                ExecutionFrequency = new TimeSpan(12,0,0),
                WorkMethod = () => { _ = exifService.CleanUpKeywordOperations(keywordCleanupFreq); },
                ImmediateStart = false
            });

            // Dump performance stats out to the logfile
            tasks.Add(new ScheduledTask
            {
                Type = ScheduledTask.TaskType.DumpPerformance,
                ExecutionFrequency = new TimeSpan(24, 0, 0),
                WorkMethod = () => {
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
}
