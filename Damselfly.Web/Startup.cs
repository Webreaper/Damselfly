using System;
using System.IO;
using Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Blazored.Modal;
using Damselfly.Web.Data;
using Damselfly.Core.Services;
using System.Collections.Generic;
using Damselfly.Core.Models;
using Tewr.Blazor.FileReader;
using Radzen;
using Damselfly.Core.Utils;

namespace Damselfly.Web
{
    /// <summary>
    /// Core initialisation and management of the startup of the app.
    /// Responsible for bootstrapping all the services, and setting
    /// up the scheduled tasks that we'll want to run.
    /// </summary>
    public class Startup
    {
        private static TaskService taskScheduler;

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
            services.AddLogging();
            services.AddResponseCaching();
            services.AddRazorPages();
            services.AddBlazoredModal();
            services.AddServerSideBlazor();
            services.AddFileReaderService();
            services.AddSingleton<ImageService>();
            services.AddSingleton(StatusService.Instance);
            services.AddSingleton(SearchService.Instance);
            services.AddSingleton(ThumbnailService.Instance);
            services.AddSingleton(FolderService.Instance);
            services.AddSingleton(TaskService.Instance);
            services.AddSingleton(DownloadService.Instance);
            services.AddSingleton(IndexingService.Instance);
            services.AddSingleton(BasketService.Instance);
            services.AddSingleton(MetaDataService.Instance);
            services.AddSingleton(ImageProcessService.Instance);
            services.AddSingleton(WordpressService.Instance);
            services.AddSingleton(SelectionService.Instance);
            services.AddSingleton<NavigationService>();
            services.AddSingleton<ViewDataService>();
            services.AddSingleton<ConfigService>();
            services.AddScoped<ContextMenuService>();
        }

        /// <summary>
        /// Called by the Blazor runtime - this is where we setup the HTTP request pipeline and
        /// initialise all the bits and pieces we need to run.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if( Logging.Verbose )
                app.UseSerilogRequestLogging(); // <-- Add this line

            // Disable this for now
            // app.UseHttpsRedirection();

            // TODO: Do we need this if we serve all the images via the controller?
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(ThumbnailService.PicturesRoot),
                RequestPath = ThumbnailService.RequestRoot
            });

            app.UseRouting();
            app.UseResponseCaching();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller}/{action}");
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            string contentRootPath = Path.Combine(env.ContentRootPath, "wwwroot");

            DownloadService.Instance.SetDownloadPath(contentRootPath);

            // TODO: Fix this, or not if Skia doesn't need it
            ImageProcessService.Instance.SetContentPath( contentRootPath );

            Logging.Log("Preloading config, folders, images and selection...");

            // TODO: Make all this async?
            ConfigService.Instance.InitialiseCache();
            SearchService.Instance.PreLoadSearchData();
            FolderService.Instance.PreLoadFolderData();
            BasketService.Instance.Initialise();
            MetaDataService.Instance.StartService();

            if (IndexingService.EnableIndexing)
                IndexingService.Instance.StartService();

            if (IndexingService.EnableThumbnailGeneration)
                ThumbnailService.Instance.StartService();

            Logging.Log("Preloading complete");

            StartTaskScheduler();
        }

        /// <summary>
        /// Bootstrap the task scheduler - configuring all the background scheduled tasks
        /// that we'll want to run periodically, such as indexing, thumbnail generation,
        /// cleanup of temporary download files, etc., etc.
        /// </summary>
        private static void StartTaskScheduler()
        {
            taskScheduler = TaskService.Instance;

            var tasks = new List<ScheduledTask>();

            // Clean up old download zips from the wwwroot folder
            var downloadCleanupFreq = new TimeSpan(6, 0, 0);
            tasks.Add(new ScheduledTask
            {
                Type = ScheduledTask.TaskType.CleanupDownloads,
                ExecutionFrequency = downloadCleanupFreq,
                WorkMethod = () => DownloadService.Instance.CleanUpOldDownloads(downloadCleanupFreq),
                ImmediateStart = true
            });

            // Purge keyword operation entries that have been processed
            var keywordCleanupFreq = new TimeSpan(24, 0, 0);
            tasks.Add(new ScheduledTask
            {
                Type = ScheduledTask.TaskType.CleanupKeywordOps,
                ExecutionFrequency = new TimeSpan(24,0,0),
                WorkMethod = () => MetaDataService.Instance.CleanUpKeywordOperations(keywordCleanupFreq).Wait(),
                ImmediateStart = true
            });

            // Dump performance stats out to the logfile
            tasks.Add( new ScheduledTask
            {
                Type = ScheduledTask.TaskType.DumpPerformance,
                ExecutionFrequency = new TimeSpan(12, 0, 0),
                WorkMethod = () => Stopwatch.WriteTotals(false)
            });

            // Flush the DB WriteCache (currently a no-op except for SQLite
            /*
            tasks.Add( new ScheduledTask
            {
                Type = ScheduledTask.TaskType.FlushDBWriteCache,
                ExecutionFrequency = new TimeSpan(2, 0, 0),
                WorkMethod = () =>
                {
                    using (var db = new ImageContext())
                    {
                        db.FlushDBWriteCache();
                    }
                }
            });
            */

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
