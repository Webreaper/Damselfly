using System;
using System.Reflection;
using Serilog;
using System.IO;
using System.Runtime.InteropServices;
using CommandLine;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Damselfly.Core.Services;
using Damselfly.Core.Models;
using Damselfly.Core.Interfaces;
using Damselfly.Core.DBAbstractions;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Builder;
using Damselfly.Areas.Identity;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Tewr.Blazor.FileReader;
using Syncfusion.Blazor;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity.UI.Services;
using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices;
using Damselfly.ML.ObjectDetection;
using Damselfly.Shared.Utils;
using MailKit;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.FileProviders;

namespace Damselfly.Web
{
    /// <summary>
    /// Bootstrap and command-line parameters for the app.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Can't see this being used much around the web so let's
        /// stake a claim for 6363 as our default port. :)
        /// </summary>
        private static int s_defaultPort = 6363;

        public class DamselflyOptions
        {
            [Value(0, MetaName = "Source Directory", HelpText = "Base folder for photographs.", Required = true)]
            public string SourceDirectory { get; set; }

            [Option("config", HelpText = "Config path", Required = false)]
            public string ConfigPath { get; set; } = "./config";

            [Option("thumbs", HelpText = "Thumbnail cache path (ignored if --syno specified)", Required = false)]
            public string ThumbPath { get; set; } = "./config/thumbs";

            [Option('v', "verbose", HelpText = "Run logging in Verbose Mode")]
            public bool Verbose { get; set; }

            [Option('t', "trace", HelpText = "Enable Trace logging mode")]
            public bool Trace { get; set; }

            [Option('r', "readonly", HelpText = "Enable Read-Only mode for database")]
            public bool ReadOnly { get; set; }

            [Option("port", HelpText = "Port for Webserver (default = 6363)", Required = false)]
            public int Port { get; set; } = s_defaultPort;

            [Option("syno", Required = false, HelpText = "Use native Synology thumbnail structure.")]
            public bool Synology { get; set; }

            [Option("nothumbs", Required = false, HelpText = "Don't Generate thumbnails")]
            public bool NoGenerateThumbnails { get; set; }

            [Option("noindex", Required = false, HelpText = "Don't Index images")]
            public bool NoEnableIndexing { get; set; }

            [Option("postgres", Required = false, HelpText = "Use Postgres DB (default == Sqlite)")]
            public bool UsePostgresDB { get; set; }
        };

        public static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<DamselflyOptions>(args).WithParsed(o =>
                           {
                               Startup(o, args);
                           });
            }
            catch( Exception ex )
            {
                Console.WriteLine($"Startup exception: {ex}");
            }
        }

        /// <summary>
        /// Process the startup args and initialise the logging.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private static void Startup(DamselflyOptions o, string[] args)
        {
            Logging.Verbose = o.Verbose;
            Logging.Trace = o.Trace;

            if (Directory.Exists(o.SourceDirectory))
            {
                if (!Directory.Exists(o.ConfigPath))
                    Directory.CreateDirectory(o.ConfigPath);

                if (o.ReadOnly)
                {
                    o.NoEnableIndexing = true;
                    o.NoGenerateThumbnails = true;
                }

                // TODO: Do away with static members here. We should pass this
                // through to the config service and pick them up via DI
                IndexingService.EnableIndexing = !o.NoEnableIndexing;
                IndexingService.RootFolder = o.SourceDirectory;
                ThumbnailService.PicturesRoot = o.SourceDirectory;
                ThumbnailService.Synology = o.Synology;
                ThumbnailService.SetThumbnailRoot(o.ThumbPath);
                ThumbnailService.EnableThumbnailGeneration = !o.NoGenerateThumbnails;

                var tieredPGO = System.Environment.GetEnvironmentVariable("DOTNET_TieredPGO") == "1";

                Logging.Log("Startup State:");
                Logging.Log($" Damselfly Ver: {Assembly.GetExecutingAssembly().GetName().Version}");
                Logging.Log($" CLR Ver: {Environment.Version}");
                Logging.Log($" OS: {Environment.OSVersion}");
                Logging.Log($" CPU Arch: {RuntimeInformation.ProcessArchitecture}");
                Logging.Log($" Processor Count: {Environment.ProcessorCount}");
                Logging.Log($" Read-only mode: {o.ReadOnly}");
                Logging.Log($" Synology = {o.Synology}");
                Logging.Log($" Indexing = {!o.NoEnableIndexing}");
                Logging.Log($" ThumbGen = {!o.NoGenerateThumbnails}");
                Logging.Log($" Images Root set as {o.SourceDirectory}");
                Logging.Log($" TieredPGO Enabled={tieredPGO}");

                // Make ourselves low-priority.
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Idle;

                StartWebServer(o, args);

                Logging.Log("Shutting down.");
            }
            else
            {
                Console.WriteLine("Folder {0} did not exist. Exiting.", o.SourceDirectory);
            }

        }

        private static void SetupDbContext(WebApplicationBuilder builder, DamselflyOptions cmdLineOptions)
        {
            string dbFolder = Path.Combine(cmdLineOptions.ConfigPath, "db");

            if (!Directory.Exists(dbFolder))
            {
                Logging.Log(" Created DB folder: {0}", dbFolder);
                Directory.CreateDirectory(dbFolder);
            }

            string dbPath = Path.Combine(dbFolder, "damselfly.db");

            string connectionString = $"Data Source={dbPath}";

            // Add services to the container.
            builder.Services.AddDbContext<ImageContext>(options => options.UseSqlite(connectionString,
                                                        b => b.MigrationsAssembly("Damselfly.Migrations.Sqlite")));
        }

        private static void InitialiseDB(WebApplication app, DamselflyOptions options)
        {
            using var scope = app.Services.CreateScope();
            using var db = scope.ServiceProvider.GetService<ImageContext>();

            try
            {
                Logging.Log("Running Sqlite DB migrations...");
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                Logging.LogWarning($"Migrations failed with exception: {ex}");

                if (ex.InnerException != null)
                    Logging.LogWarning($"InnerException: {ex.InnerException}");

                Logging.Log($"Creating DB.");
                db.Database.EnsureCreated();
            }

            db.IncreasePerformance();

            ImageContext.ReadOnly = options.ReadOnly;
        }

        /// <summary>
        /// Main entry point. Creates a bunch of services, and then kicks off
        /// the webserver, which is a blocking call (since it's the dispatcher
        /// thread) until the app exits.
        /// </summary>
        /// <param name="listeningPort"></param>
        /// <param name="args"></param>
        private static void StartWebServer(DamselflyOptions cmdLineOptions, string[] args )
        {
            try
            {
                Logging.Log("Initialising Damselfly...");

                var builder = WebApplication.CreateBuilder(args);

                var services = builder.Services;

                var logFolder = Path.Combine(cmdLineOptions.ConfigPath, "logs");

                builder.Host.UseSerilog((hostContext, services, configuration) => {
                    Logging.InitLogConfiguration( configuration, logFolder );
                });

                builder.Services.AddLogging();
                builder.Services.AddResponseCompression();
                builder.Services.AddResponseCaching();
                builder.Services.AddRazorPages();
                builder.Services.AddServerSideBlazor();
                builder.Services.AddFileReaderService();

                builder.Services.AddMudServices();
                builder.Services.AddSyncfusionBlazor();

                // Cache up to 10,000 images. Should be enough given cache expiry.
                builder.Services.AddMemoryCache(x => x.SizeLimit = 5000);

                builder.Services.ConfigureApplicationCookie(options => options.Cookie.Name = "Damselfly");

                SetupDbContext(builder, cmdLineOptions);

                builder.Services.AddDataProtection().PersistKeysToDbContext<ImageContext>();

                builder.Services.AddDefaultIdentity<AppIdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
                                     .AddRoles<ApplicationRole>()
                                     .AddEntityFrameworkStores<ImageContext>();

                // Use transient here so that if the user preferences change, 
                // we'll get a different instance the next time we send email. 
                builder.Services.AddTransient<IEmailSender, EmailSenderFactoryService>();

                builder.Services.AddSingleton<IConfigService>(x => x.GetRequiredService<ConfigService>());
                builder.Services.AddSingleton<ITransactionThrottle>(x => x.GetRequiredService<TransThrottle>());

                builder.Services.AddImageServices();
                builder.Services.AddHostedBlazorBackEndServices();

                // Radzen
                builder.Services.AddScoped<ContextMenuService>();

                // This needs to happen after ConfigService has been registered.
                builder.Services.AddAuthorization(config => config.SetupPolicies(services));

                builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<AppIdentityUser>>();

                builder.Services.AddBlazorServerScopedServices();

                var app = builder.Build();

                Logging.Logger = app.Services.GetRequiredService<ILogger>();
                Logging.Logger.Information("=== Damselfly Hosted Server Log Started ===");

                InitialiseDB(app, cmdLineOptions);

                SyncfusionLicence.RegisterSyncfusionLicence();

                var configService = app.Services.GetRequiredService<ConfigService>();
                var logLevel = configService.Get(ConfigSettings.LogLevel, Serilog.Events.LogEventLevel.Information);

                Logging.ChangeLogLevel(logLevel);

                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                }

                if (Logging.Verbose)
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

                app.Environment.SetupServices(app.Services);

                Logging.StartupCompleted();
                Logging.Log("Starting Damselfly webserver...");

                app.Urls.Add($"http://+:{cmdLineOptions.Port}");

                app.Environment.SetupServices(app.Services);

                app.Run();

                Logging.LogWarning("Damselfly Webserver stopped. Exiting");
            }
            catch ( Exception ex )
            {
                Logging.Log("Damselfly Webserver terminated with exception: {0}", ex.Message);
            }

        }
    }
}
        