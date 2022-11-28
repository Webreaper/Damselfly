using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using CommandLine;
using Damselfly.Core.Constants;
using Damselfly.Core.DBAbstractions;
using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.Services;
using Damselfly.Core.Utils;
using Damselfly.Shared.Utils;
using Damselfly.Web.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace Damselfly.Web;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Parser.Default.ParseArguments<DamselflyOptions>(args).WithParsed(o => { Startup(o, args); });
        }
        catch ( Exception ex )
        {
            Console.WriteLine($"Startup exception: {ex}");
        }
    }

    /// <summary>
    ///     Process the startup args and initialise the logging.
    /// </summary>
    /// <param name="o"></param>
    /// <param name="args"></param>
    private static void Startup(DamselflyOptions o, string[] args)
    {
        Logging.Verbose = o.Verbose;
        Logging.Trace = o.Trace;

        if ( Directory.Exists(o.SourceDirectory) )
        {
            if ( !Directory.Exists(o.ConfigPath) )
                Directory.CreateDirectory(o.ConfigPath);

            if ( o.ReadOnly )
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

            var tieredPGO = Environment.GetEnvironmentVariable("DOTNET_TieredPGO") == "1";

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
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;

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
        var dbFolder = Path.Combine(cmdLineOptions.ConfigPath, "db");

        if ( !Directory.Exists(dbFolder) )
        {
            Logging.Log(" Created DB folder: {0}", dbFolder);
            Directory.CreateDirectory(dbFolder);
        }

        var dbPath = Path.Combine(dbFolder, "damselfly.db");

        var connectionString = $"Data Source={dbPath}";

        // Add services to the container.
        builder.Services.AddDbContext<ImageContext>(options => options.UseSqlite(connectionString,
            b => {
                b.MigrationsAssembly("Damselfly.Migrations.Sqlite");
                b.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            }));
    }

    /// <summary>
    ///     Main entry point. Creates a bunch of services, and then kicks off
    ///     the webserver, which is a blocking call (since it's the dispatcher
    ///     thread) until the app exits.
    /// </summary>
    /// <param name="listeningPort"></param>
    /// <param name="args"></param>
    private static void StartWebServer(DamselflyOptions cmdLineOptions, string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var logFolder = Path.Combine(cmdLineOptions.ConfigPath, "logs");

        builder.Host.UseSerilog((hostContext, services, configuration) =>
        {
            Logging.InitLogConfiguration(configuration, logFolder);
        });

        SetupDbContext(builder, cmdLineOptions);

        SetupIdentity(builder.Services);

        // Cache up to 10,000 images. Should be enough given cache expiry.
        builder.Services.AddMemoryCache(x => x.SizeLimit = 5000);

        builder.Services.AddControllers()
            .AddJsonOptions(o => { RestClient.SetJsonOptions(o.JsonSerializerOptions); });
        builder.Services.AddControllersWithViews()
            .AddJsonOptions(o => { RestClient.SetJsonOptions(o.JsonSerializerOptions); });
        builder.Services.AddRazorPages()
            .AddJsonOptions(o => { RestClient.SetJsonOptions(o.JsonSerializerOptions); });

        // Server to client notifications
        builder.Services.AddSignalR();
        builder.Services.AddResponseCompression(opts =>
        {
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "image/svg+xml",  "application/octet-stream" });
        });

        // Damselfly Services
        builder.Services.AddImageServices();
        builder.Services.AddHostedBlazorBackEndServices();

        // Use Kestrel options to set the port. Using .Urls.Add breaks WASM debugging.
        builder.WebHost.UseKestrel(serverOptions => { serverOptions.ListenAnyIP(cmdLineOptions.Port); });

        var app = builder.Build();

        Logging.Logger = app.Services.GetRequiredService<ILogger>();
        Logging.Logger.Information("=== Damselfly Blazor Server Log Started ===");

        InitialiseDB(app, cmdLineOptions);

        // Log ingestion from the client
        app.UseSerilogIngestion();

        var configService = app.Services.GetRequiredService<ConfigService>();
        var logLevel = configService.Get(ConfigSettings.LogLevel, LogEventLevel.Information);

        Logging.ChangeLogLevel(logLevel);

        // Configure the HTTP request pipeline.
        if ( app.Environment.IsDevelopment() )
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

        app.UseHttpsRedirection();

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
        app.UseEndpoints(ep => { ep.MapHub<NotificationHub>($"/{NotificationHub.NotificationRoot}"); });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        // Start up all the Damselfly Services
        app.Environment.SetupServices(app.Services);

        Logging.StartupCompleted();
        Logging.Log("Starting Damselfly webserver...");

        app.Run();
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
        catch ( Exception ex )
        {
            Logging.LogWarning($"Migrations failed with exception: {ex}");

            if ( ex.InnerException != null )
                Logging.LogWarning($"InnerException: {ex.InnerException}");

            Logging.Log("Creating DB.");
            db.Database.EnsureCreated();
        }

        db.IncreasePerformance();

        BaseDBModel.ReadOnly = options.ReadOnly;
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
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes("BlahSomeKeyBlahFlibbertyGibbertNonsenseBananarama"))
                };
            });

        services.AddAuthorization(config => config.SetupPolicies(services));
    }
}