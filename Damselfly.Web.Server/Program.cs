using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using CommandLine;
using static Tensorflow.ApiDef.Types;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using Damselfly.Core.Utils;
using Damselfly.Core.ImageProcessing;
using Damselfly.Core.DBAbstractions;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Damselfly.Core.Interfaces;
using Damselfly.Core.DbModels;
using Damselfly.Web.Server;
using Damselfly.Shared.Utils;
using Damselfly.Migrations.Sqlite.Models;
using Damselfly.Migrations.Postgres.Models;
using Damselfly.Core.Constants;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Syncfusion.Licensing;
using Damselfly.Core.ScopedServices.ClientServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    }

    /// <summary>
    /// Main entry point. Creates a bunch of services, and then kicks off
    /// the webserver, which is a blocking call (since it's the dispatcher
    /// thread) until the app exits.
    /// </summary>
    /// <param name="listeningPort"></param>
    /// <param name="args"></param>
    private static void StartWebServer(DamselflyOptions cmdLineOptions, string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        SetupDbContext(builder, cmdLineOptions);

        SetupIdentity(builder.Services);

        // Cache up to 10,000 images. Should be enough given cache expiry.
        builder.Services.AddMemoryCache(x => x.SizeLimit = 5000);

        builder.Services.AddControllersWithViews()
                .AddJsonOptions(o => { RestClient.SetJsonOptions(o.JsonSerializerOptions); });

        builder.Services.AddRazorPages();

#if DEBUG
        builder.Services.AddSwaggerGen();
#endif

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

        var app = builder.Build();

        var configService = app.Services.GetRequiredService<ConfigService>();
        var logLevel = configService.Get(ConfigSettings.LogLevel, Serilog.Events.LogEventLevel.Information);

        Logging.ChangeLogLevel(logLevel);

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

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        InitialiseDB(app, cmdLineOptions);

        // Start up all the Damselfly Services
        app.Environment.SetupServices(app.Services);

        app.Urls.Add($"http://+:{cmdLineOptions.Port}");

        Logging.StartupCompleted();
        Logging.Log("Starting Damselfly webserver...");

        app.Run();
    }

    private static void InitialiseDB( WebApplication app, DamselflyOptions options)
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

    private static void SetupIdentity(IServiceCollection services)
    {
        services.AddDefaultIdentity<AppIdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ImageContext>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
           .AddJwtBearer(options =>
           {
               options.TokenValidationParameters = new TokenValidationParameters
               {
                   ValidateIssuer = true,
                   ValidateAudience = true,
                   ValidateLifetime = true,
                   ValidateIssuerSigningKey = true,
                   ValidIssuer = "https://localhost",
                   ValidAudience = "https://localhost",
                   IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("BlahSomeKeyBlahFlibbertyGibbertNonsenseBananarama"))
               };
           });

        services.AddAuthorization(config => config.SetupPolicies(services));
    }
}

