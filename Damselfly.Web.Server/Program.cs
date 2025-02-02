using System.Diagnostics;
using Damselfly.Core.Constants;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Services;
using Damselfly.Core.Utils;
using Damselfly.Shared.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Hangfire;
using Hangfire.MemoryStorage;
using Damselfly.PaymentProcessing;

namespace Damselfly.Web;

public class Program
{
    private static BackgroundJobServer backgroundJobServer;
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

        var connectionString = builder.Configuration["DamselflyConfiguration:ConnectionString"];

        // Add services to the container.
        builder.Services.AddDbContext<ImageContext>(options => {
            options.UseNpgsql(connectionString,
                       b =>
                       {
                           b.MigrationsAssembly("Damselfly.Migrations.Postgres");
                           b.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                       });
            
        }, ServiceLifetime.Transient);

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
             });

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
        
        builder.Services.AddHangfire(config =>
        {
            config.UseSerilogLogProvider();
            config.UseMemoryStorage();
        });
        // builder.Services.AddHangfireServer();

        // Swagger
        builder.Services.AddSwaggerGen();

        // Damselfly Services
        builder.Services.AddImageServices();
        builder.Services.AddPaymentServices();
        builder.Services.AddHostedBlazorBackEndServices();
        var port = int.Parse(builder.Configuration["DamselflyConfiguration:Port"]);
        if( ! Debugger.IsAttached )
        {
            // Use Kestrel options to set the port. Using .Urls.Add breaks WASM debugging.
            // This line also breaks wasm debugging in Rider.
            // See https://github.com/dotnet/aspnetcore/issues/43703
            builder.WebHost.UseKestrel(serverOptions => { serverOptions.ListenAnyIP(port); });
        }
        var allowedOrigins = builder.Configuration["DamselflyConfiguration:AllowedOrigins"]?.Split(",");
        const string allowAllorigins = "AllowAllOrigins";
        const string allowSpecificOrigins = "AllowSpecificOrigins";
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                allowAllorigins,
                builder =>
                {
                    builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                }
            );
            options.AddPolicy(
                allowSpecificOrigins,
                builder =>
                {
                    builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });
        });

        var app = builder.Build();

        Logging.Logger = app.Services.GetRequiredService<ILogger>();
        // ogging.Logger.Information("=== Damselfly Blazor Server Log Started ===");
        Logging.Log("Starting up Damselfly webserver...");
        Logging.Log("Log directory: {0}", logFolder);
        var currentDirectory = Directory.GetCurrentDirectory();
        Logging.Log("Current directory: {0}", currentDirectory);
        InitialiseDB(app);

        //// Log ingestion from the client
        //app.UseSerilogIngestion();

        var configService = app.Services.GetRequiredService<ConfigService>();
        var logLevelString = builder.Configuration["Logging:LogLevel:Default"];  // configService.Get(ConfigSettings.LogLevel, LogEventLevel.Information);
        var logLevel = LogEventLevel.Information;
        if( Enum.TryParse<LogEventLevel>(logLevelString, true, out var parsedLevel) )
            logLevel = parsedLevel;

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
#if DEBUG
        app.UseCors(allowAllorigins);
#else
        app.UseCors(allowSpecificOrigins);
#endif

        if( Debugger.IsAttached )
        {
            app.UseSwagger();
            app.UseSwaggerUI( c =>
            {
                c.SwaggerEndpoint( "/swagger/v1/swagger.json", "Damselfly API" );
            } );
        }

        // Map the signalR notifications endpoints

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapControllers();
        app.MapHub<ImageDownloadHub>("/imageDownloadHub");
        app.MapFallbackToFile("index.html");
        
        // Start up all the Damselfly Services
        app.Environment.SetupServices(app.Services);

        app.UseHangfireServer();


        // BackgroundJob.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));
        // BackgroundJob.Enqueue<DownloadService>(x => x.CleanUpOldDownloads(TimeSpan.FromHours(6)));
        RecurringJob.AddOrUpdate<DownloadService>("CleanupDownloads", d => d.CleanUpOldDownloads(TimeSpan.FromHours(6)), "0 */6 * * *");
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
                Logging.Log( "Running DB migrations..." );
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

            //db.IncreasePerformance();

            //BaseDBModel.ReadOnly = app.Configuration["DamselflyConfiguration:Readonly"] == "true";
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
        var serviceProvider = services.BuildServiceProvider();
        var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        services.AddAuthorization(config => config.AddPolicy(PolicyDefinitions.s_FireBaseAdmin, policy => policy.Requirements.Add(new AuthorizeFireBase(httpContextAccessor))));
        // services.AddAuthorization(config => config.SetupPolicies(services));

        //services.AddSingleton<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthorizationRequirement, AuthorizeFireBase>();
    }
}