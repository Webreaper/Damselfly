using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using CommandLine;
using Damselfly.Core.Constants;
using Damselfly.Core.Database;
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
using Damselfly.Core.ScopedServices.Interfaces;
using Tensorflow;

namespace Damselfly.Web;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            // Parser.Default.ParseArguments<DamselflyOptions>(args).WithParsed(o => { Startup(o, args); });
            StartWebServer(args);
        }
        catch ( Exception ex )
        {
            Console.WriteLine($"Startup exception: {ex}");
        }
    }

    private static void SetupDbContext(WebApplicationBuilder builder)
    {
        var dbFolder = Path.Combine(builder.Configuration["DamselflyConfiguration:DatabasePath"], "db");

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
    private static void StartWebServer(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var logdirectory = builder.Configuration["DamselflyConfiguration:LogPath"];

        var logFolder = Path.Combine(logdirectory, "logs");
        Logging.Verbose = builder.Configuration["DamselflyConfiguration:Verbose"] == "true";
        Logging.Trace = builder.Configuration["DamselflyConfiguration:Trace"] == "true";

        builder.Host.UseSerilog((hostContext, services, configuration) =>
        {
            Logging.InitLogConfiguration(configuration, logFolder);
        });

        SetupDbContext(builder);

        SetupIdentity(builder.Services, builder.Configuration);

        

        // Cache up to 10,000 images. Should be enough given cache expiry.
        builder.Services.AddMemoryCache(x => x.SizeLimit = 5000);

        builder.Services.AddControllers()
            .AddJsonOptions(o => { 
                o.JsonSerializerOptions.AllowTrailingCommas = true;
                o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
                RestClient.SetJsonOptions(o.JsonSerializerOptions); });
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

        builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


        // Swagger
        builder.Services.AddSwaggerGen();

        // Damselfly Services
        builder.Services.AddImageServices();
        builder.Services.AddHostedBlazorBackEndServices();
        var port = int.Parse(builder.Configuration["DamselflyConfiguration:Port"]);
        if( ! Debugger.IsAttached )
        {
            // Use Kestrel options to set the port. Using .Urls.Add breaks WASM debugging.
            // This line also breaks wasm debugging in Rider.
            // See https://github.com/dotnet/aspnetcore/issues/43703
            builder.WebHost.UseKestrel(serverOptions => { serverOptions.ListenAnyIP(port); });
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowAllOrigins",
                builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                }
            );
        });

        var app = builder.Build();

        Logging.Logger = app.Services.GetRequiredService<ILogger>();
        // ogging.Logger.Information("=== Damselfly Blazor Server Log Started ===");

        InitialiseDB(app);

        // Log ingestion from the client
        app.UseSerilogIngestion();

        var configService = app.Services.GetRequiredService<ConfigService>();
        var logLevel = configService.Get(ConfigSettings.LogLevel, LogEventLevel.Information);


        if( app.Configuration["DamselflyConfiguration:NoGenerateThumbnails"] == "true" )
            configService.Set( ConfigSettings.EnableBackgroundThumbs, false.ToString() );
        

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

        app.UseBlazorFrameworkFiles();
        
        // TODO: Do we need this if we serve all the images via the controller?
        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(app.Configuration["DamselflyConfiguration:SourceDirectory"]),
            RequestPath = "/download" // ThumbnailService.RequestRoot
        });

        app.UseStaticFiles();
        app.UseResponseCompression();
        app.UseRouting();
        app.UseAntiforgery();
        app.UseCors("AllowAllOrigins");

        if( Debugger.IsAttached )
        {
            app.UseSwagger();
            app.UseSwaggerUI( c =>
            {
                c.SwaggerEndpoint( "/swagger/v1/swagger.json", "Damselfly API" );
            } );
        }

        // Map the signalR notifications endpoints
        app.MapHub<NotificationHub>($"/{NotificationHub.NotificationRoot}", options => options.AllowStatefulReconnects = true );

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

    private static void InitialiseDB(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        if( db != null )
        {
            try
            {
                Logging.Log( "Running Sqlite DB migrations..." );
                db.Database.Migrate();
            }
            catch( Exception ex )
            {
                Logging.LogWarning( $"Migrations failed with exception: {ex}" );

                if( ex.InnerException != null )
                    Logging.LogWarning( $"InnerException: {ex.InnerException}" );

                Logging.Log( "Creating DB." );
                db.Database.EnsureCreated();
            }

            db.IncreasePerformance();

            BaseDBModel.ReadOnly = app.Configuration["DamselflyConfiguration:Readonly"] == "true";
        }
    }

    private static void SetupIdentity(IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddDefaultIdentity<AppIdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ImageContext>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Audience = configuration["Jwt:Firebase:ValidAudience"];
                options.Authority = configuration["Jwt:Firebase:ValidIssuer"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Firebase:ValidIssuer"],
                    ValidAudience = configuration["Jwt:Firebase:ValidAudience"]
                };
            });

        services.AddAuthorization(config => config.SetupPolicies(services));

        //services.AddSingleton<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
        services.AddScoped<IAuthService, AuthService>();
    }
}