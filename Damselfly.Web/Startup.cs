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
using Damselfly.Web.Data;
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
using Damselfly.Core.Utils.Constants;
using Microsoft.AspNetCore.DataProtection;
using Syncfusion.Blazor;

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
            services.AddLogging();
            services.AddResponseCompression();
            services.AddResponseCaching();
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddFileReaderService();
            services.AddMudServices();

            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NTUxMzEwQDMxMzkyZTM0MmUzMGFRSFpzQUhjdUE2M2V4S1BmYSs5bk13dkpGbkhvam5Wb1VRbGVURkRsOHM9");
            services.AddSyncfusionBlazor();

            // Cache up to 10,000 images. Should be enough given cache expiry.
            services.AddMemoryCache( x => x.SizeLimit = 10000 );

            services.AddDbContext<ImageContext>();
            services.ConfigureApplicationCookie(options => options.Cookie.Name = "Damselfly");

            services.AddDataProtection().PersistKeysToDbContext<ImageContext>();

            services.AddDefaultIdentity<AppIdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
                                 .AddRoles<ApplicationRole>()
                                 .AddEntityFrameworkStores<ImageContext>();

            // Use transient here so that if the user preferences change, 
            // we'll get a different instance the next time we send email. 
            services.AddTransient<IEmailSender, EmailSenderFactoryService>();

            services.AddSingleton<TransThrottle>(new TransThrottle(CloudTransaction.TransactionType.AzureFace));
            services.AddSingleton<ITransactionThrottle>(x => x.GetRequiredService<TransThrottle>());
            services.AddSingleton<ConfigService>();
            services.AddSingleton<IConfigService>(x => x.GetRequiredService<ConfigService>());
            services.AddSingleton<ImageProcessorFactory>();
            services.AddSingleton<IImageProcessorFactory>(x => x.GetRequiredService<ImageProcessorFactory>());
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
            services.AddSingleton<ImageProcessService>();
            services.AddSingleton<WordpressService>();
            services.AddSingleton<AccordFaceService>();
            services.AddSingleton<AzureFaceService>();
            services.AddSingleton<ImageClassifier>();
            services.AddSingleton<EmguFaceService>();
            services.AddSingleton<ThemeService>();
            services.AddSingleton<ImageRecognitionService>();
            services.AddSingleton<ImageCache>();
            services.AddSingleton<WorkService>();

            // This needs to happen after ConfigService has been registered.
            services.AddAuthorization(config => SetupPolicies(config, services));

            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<AppIdentityUser>>();
            services.AddScoped<BasketService>();
            services.AddScoped<UserService>();
            services.AddScoped<UserStatusService>();
            services.AddScoped<UserConfigService>();
            services.AddScoped<SearchService>();
            services.AddScoped<NavigationService>();
            services.AddScoped<UserFolderService>();
            services.AddScoped<ViewDataService>();
            services.AddScoped<UserThemeService>();
            services.AddScoped<SelectionService>();
            services.AddScoped<ContextMenuService>();
            services.AddScoped<UserTagFavouritesService>();
        }

        private void SetupPolicies(AuthorizationOptions config, IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var configService = serviceProvider.GetService<ConfigService>();

            var enablePolicies = configService.GetBool(ConfigSettings.EnablePoliciesAndRoles);

            if( enablePolicies )
            {
                Logging.Log("Polices and Roles are enabled.");

                // Anyone in the Admin group is an admin (d'uh)
                config.AddPolicy(PolicyDefinitions.s_IsLoggedIn, policy => policy.RequireAuthenticatedUser());

                // Anyone in the Admin group is an admin (d'uh)
                config.AddPolicy(PolicyDefinitions.s_IsAdmin, policy => policy.RequireRole(
                                                                RoleDefinitions.s_AdminRole));

                // Users and Admins can edit content (keywords)
                config.AddPolicy(PolicyDefinitions.s_IsEditor, policy => policy.RequireRole(
                                                                RoleDefinitions.s_AdminRole,
                                                                RoleDefinitions.s_UserRole));
                // Admins, Users and ReadOnly users can download
                config.AddPolicy(PolicyDefinitions.s_IsDownloader, policy => policy.RequireRole(
                                                                RoleDefinitions.s_AdminRole,
                                                                RoleDefinitions.s_UserRole,
                                                                RoleDefinitions.s_ReadOnlyRole));
            }
            else
            {
                Logging.Log("Polices and Roles have been deactivated.");

                config.AddPolicy(PolicyDefinitions.s_IsLoggedIn, policy => policy.RequireAssertion(
                                                                    context => true));
                config.AddPolicy(PolicyDefinitions.s_IsAdmin, policy => policy.RequireAssertion(
                                                                    context => true));
                config.AddPolicy(PolicyDefinitions.s_IsEditor, policy => policy.RequireAssertion(
                                                                context => true));
                config.AddPolicy(PolicyDefinitions.s_IsDownloader, policy => policy.RequireAssertion(
                                                                context => true));
            }

        }

        /// <summary>
        /// Called by the Blazor runtime - this is where we setup the HTTP request pipeline and
        /// initialise all the bits and pieces we need to run.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
                        DownloadService download, ThemeService themes, TaskService tasks,
                        ExifService exifService, ThumbnailService thumbService,
                        IndexingService indexService, ImageProcessService imageProcessing,
                        AzureFaceService azureFace, ImageRecognitionService aiService,
                        UserService userService, ConfigService configService, WorkService workService,
                        ImageCache imageCache,  MetaDataService metaDataService, ObjectDetector objectDetector)
        {
            var logLevel = configService.Get(ConfigSettings.LogLevel, Serilog.Events.LogEventLevel.Information);

            Logging.ChangeLogLevel( logLevel );

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
                app.UseSerilogRequestLogging();

            app.UseResponseCompression();
            app.UseRouting();
            app.UseResponseCaching();

            // Disable this for now
            // app.UseHttpsRedirection();

            // TODO: Do we need this if we serve all the images via the controller?
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(ThumbnailService.PicturesRoot),
                RequestPath = ThumbnailService.RequestRoot
            });

            // Enable auth
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllerRoute(name: "default", pattern: "{controller}/{action}");
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            // Prime the cache
            imageCache.WarmUp().Wait();

            // TODO: Save this in ConfigService
            string contentRootPath = Path.Combine(env.ContentRootPath, "wwwroot");

            // TODO: Fix this, or not if Skia doesn't need it
            imageProcessing.SetContentPath(contentRootPath);
            download.SetDownloadPath(contentRootPath);
            themes.SetContentPath(contentRootPath);

            // Start the work processing queue for AI, Thumbs, etc
            workService.StartService();

            // Start the face service before the thumbnail service
            azureFace.StartService().Wait();
            metaDataService.StartService();
            indexService.StartService();
            aiService.StartService();

            // ObjectDetector can throw a segmentation fault if the docker container is pinned
            // to a single CPU, so for now, to aid debugging, let's not even try and initialise
            // it if AI is disabled. See https://github.com/Webreaper/Damselfly/issues/334
            if ( ! configService.GetBool(ConfigSettings.DisableObjectDetector, false) )
                objectDetector.InitScorer();

            // Validation check to ensure at least one user is an Admin
            userService.CheckAdminUser().Wait();

            StartTaskScheduler(tasks, download, thumbService, exifService);

            Logging.StartupCompleted();
            Logging.Log("Starting Damselfly webserver...");
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
                WorkMethod = () => exifService.CleanUpKeywordOperations(keywordCleanupFreq).Wait(),
                ImmediateStart = false
            });

            // Dump performance stats out to the logfile
            tasks.Add(new ScheduledTask
            {
                Type = ScheduledTask.TaskType.DumpPerformance,
                ExecutionFrequency = new TimeSpan(24, 0, 0),
                WorkMethod = () => Stopwatch.WriteTotals(false),
                ImmediateStart = false
            });

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
