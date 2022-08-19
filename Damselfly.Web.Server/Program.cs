using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Damselfly.Web.Server.Data;
using Damselfly.Web.Server.Models;
using Microsoft.Extensions.Hosting.Internal;
using Damselfly.Core.Utils;
using Damselfly.Core.ImageProcessing;
using Serilog;
using static Tensorflow.ApiDef.Types;
using CommandLine;
using Damselfly.Core.DbModels.DBAbstractions;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using System.Reflection;
using System.Runtime.InteropServices;
using Damselfly.Core.DbModels.Interfaces;
using Damselfly.Migrations.Sqlite.Models;
using Damselfly.Migrations.Postgres.Models;
using Damselfly.Core.Constants;
using Damselfly.Core.Interfaces;
using Microsoft.Extensions.FileProviders;
using Syncfusion.Licensing;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Damselfly.Core.DbModels;
using Damselfly.Web.Server;
using Damselfly.Shared.Utils;
using System.Text.Json;

namespace Damselfly.Web;

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
        catch (Exception ex)
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

            Logging.LogFolder = Path.Combine(o.ConfigPath, "logs");

            Log.Logger = Logging.InitLogs();

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

            IDataBase dbType = null;

            if (!o.UsePostgresDB)
            {
                string dbFolder = Path.Combine(o.ConfigPath, "db");

                if (!Directory.Exists(dbFolder))
                {
                    Logging.Log(" Created DB folder: {0}", dbFolder);
                    Directory.CreateDirectory(dbFolder);
                }

                string dbPath = Path.Combine(dbFolder, "damselfly.db");
                dbType = new SqlLiteModel(dbPath);
                Logging.Log(" Sqlite Database location: {0}", dbPath);
            }
            else // Postgres
            {
                // READ Postgres config json
                dbType = PostgresModel.ReadSettings("settings.json");
                Logging.Log(" Postgres Database location: {0}");
            }

            // TODO: https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
            BaseDBModel.InitDB<ImageContext>(dbType, o.ReadOnly);

            // Make ourselves low-priority.
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Idle;

            StartWebServer(o.Port, args);

            Logging.Log("Shutting down.");
        }
        else
        {
            Console.WriteLine("Folder {0} did not exist. Exiting.", o.SourceDirectory);
        }
    }

    /// <summary>
    /// Main entry point. Creates a bunch of services, and then kicks off
    /// the webserver, which is a blocking call (since it's the dispatcher
    /// thread) until the app exits.
    /// </summary>
    /// <param name="listeningPort"></param>
    /// <param name="args"></param>
    private static void StartWebServer(int listeningPort, string[] args)
    { 
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<ApplicationDbContext>();

        builder.Services.AddIdentityServer()
            .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

        builder.Services.AddAuthorization(config => config.SetupPolicies(builder.Services));

        builder.Services.AddAuthentication()
            .AddIdentityServerJwt();

        // Cache up to 10,000 images. Should be enough given cache expiry.
        builder.Services.AddMemoryCache(x => x.SizeLimit = 5000);

        builder.Services.AddControllersWithViews()
                .AddJsonOptions(o => { o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve; });

        builder.Services.AddRazorPages();

#if DEBUG
        builder.Services.AddSwaggerGen();
#endif

        builder.Services.AddRazorPages();
        builder.Services.AddSwaggerGen();

        // Server to client notifications
        builder.Services.AddSignalR();
        builder.Services.AddResponseCompression(opts =>
        {
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "application/octet-stream" });
        });

        // Damselfly Services
        builder.Services.AddImageServices();
        builder.Services.AddHostedBlazorBackEndServices();
        builder.Services.AddMLServices();

        var app = builder.Build();

        var configService = app.Services.GetRequiredService<ConfigService>();
        var logLevel = configService.Get(ConfigSettings.LogLevel, Serilog.Events.LogEventLevel.Information);

        Logging.ChangeLogLevel(logLevel);

        SyncfusionLicenseProvider.RegisterLicense("NTUxMzEwQDMxMzkyZTM0MmUzMGFRSFpzQUhjdUE2M2V4S1BmYSs5bk13dkpGbkhvam5Wb1VRbGVURkRsOHM9");

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        // Disable this for now
        // app.UseHttpsRedirection();

        // TODO: Do we need this if we serve all the images via the controller?
        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(ThumbnailService.PicturesRoot),
            RequestPath = ThumbnailService.RequestRoot
        });

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseResponseCompression();
        app.UseRouting();

        // Map the signalR notifications endpoints
        app.UseEndpoints(ep =>
        {
            ep.MapHub<NotificationHub>($"/{NotificationHub.NotificationRoot}");
        });

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Damselfly API V1");
        });

        app.UseIdentityServer();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");
        app.Environment.SetupServices( app.Services );

        app.Urls.Add("http://+:6363"); 

        Logging.StartupCompleted();
        Logging.Log("Starting Damselfly webserver...");

        app.Run();
    }
}

