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
using Damselfly.Core.DbModels.Interfaces;
using Damselfly.Migrations.Sqlite.Models;
using Damselfly.Migrations.Postgres.Models;
using Damselfly.Core.DbModels.DBAbstractions;
using Damselfly.Core.Utils;

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
            public int Port { get; set; }

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
        private static void StartWebServer(int listeningPort, string[] args )
        {
            try
            {
                Logging.Log("Initialising Damselfly...");

                var host = BuildHost(listeningPort, args);

                host.Run();

                Logging.LogWarning("Damselfly Webserver stopped. Exiting");
            }
            catch ( Exception ex )
            {
                Logging.Log("Damselfly Webserver terminated with exception: {0}", ex.Message);
            }

        }

        /// <summary>
        /// Builds the web host; sets up the port for the webserver
        /// and configures the logging etc. 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHost BuildHost(int port, string[] args)
        {
            if( port == 0 )
                port = s_defaultPort;

            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => BuildWebHost(webBuilder, port, args))
                .UseSerilog()
                .Build();
        }

        private static void BuildWebHost(IWebHostBuilder webBuilder, int port, string[] args)
        {
            string url = $"http://*:{port}";

            webBuilder.UseConfiguration(new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build())
#if DEBUG
                    .UseSetting(WebHostDefaults.DetailedErrorsKey, "true")
#endif
                    .UseStartup<Startup>()
                .UseUrls(url);
        }
    }
}
        