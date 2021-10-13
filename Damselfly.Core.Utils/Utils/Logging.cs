using System;
using System.IO;
using System.Threading;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Damselfly.Core.Utils
{
    public class Logging
    {
        /// <summary>
        /// Small Log event enricher to add the thread name to the log entries
        /// </summary>
        public class ThreadIDEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                string thread = Thread.CurrentThread.Name;
                if (string.IsNullOrEmpty(thread))
                    thread = Thread.CurrentThread.ManagedThreadId.ToString("D4");

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ThreadID", thread));
            }
        }

        public static Logger Logger { get; }
        public static string LogFolder { get; set; }
        /// <summary>
        /// True if verbose logging is enabled
        /// </summary>
        public static bool Verbose { get; set; } = false;
        /// <summary>
        /// True if trace logging is enabled
        /// </summary>
        public static bool Trace { get; set; } = false;

        private static readonly LoggingLevelSwitch consoleLogLevel = new LoggingLevelSwitch();
        private static readonly LoggingLevelSwitch logLevel = new LoggingLevelSwitch();
        private const string template = "[{Timestamp:HH:mm:ss.fff}-{ThreadID}-{Level:u3}] {Message:lj}{NewLine}{Exception}";
        private static Logger logger;


        /// <summary>
        /// Initialise logging and add the thread enricher.
        /// </summary>
        /// <returns></returns>
        public static Logger InitLogs()
        {
            try
            {
                if (!Directory.Exists(LogFolder))
                {
                    Console.WriteLine($"Creating log folder {LogFolder}");
                    Directory.CreateDirectory(LogFolder);
                }

                logLevel.MinimumLevel = LogEventLevel.Information;
                consoleLogLevel.MinimumLevel = LogEventLevel.Information;

                if (Verbose)
                    logLevel.MinimumLevel = LogEventLevel.Verbose;

                if (Trace)
                    logLevel.MinimumLevel = LogEventLevel.Debug;

                string logFilePattern = Path.Combine(LogFolder, "Damselfly-.log");

                logger = new LoggerConfiguration()
                    .Enrich.With(new ThreadIDEnricher())
                    .WriteTo.Console(outputTemplate: template,
                                    levelSwitch: consoleLogLevel)
                    .WriteTo.File(logFilePattern,
                                   outputTemplate: template,
                                   rollingInterval: RollingInterval.Day,
                                   fileSizeLimitBytes: 104857600,
                                   retainedFileCountLimit: 10,
                                   levelSwitch: logLevel)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .CreateLogger();

                logger.Information("=== Damselfly Log Started ===");
                logger.Information("Log folder: {0}", LogFolder);
                logger.Information("LogLevel: {0}", logLevel.MinimumLevel);
            }
            catch( Exception ex )
            {
                Console.WriteLine($"Unable to initialise logs: {ex}");
            }

            return logger;
        }

        /// <summary>
        /// Once we've successfully started, reduce the console log level
        /// to warnings only, so we don't overwhelm the console log.
        /// </summary>
        public static void StartupCompleted()
        {
            Log("Startup complete. Reducing console logging level to [Warning/Error].");

#if !DEBUG
            consoleLogLevel.MinimumLevel = LogEventLevel.Warning;
#endif
        }

        /// <summary>
        /// Allow runtime toggling of debug logs
        /// </summary>
        /// <param name="newLevel"></param>
        public static void ChangeLogLevel(LogEventLevel newLevel)
        {
            if (newLevel != logLevel.MinimumLevel)
            {
                logLevel.MinimumLevel = newLevel;

                logger.Information("LogLevel: {0}", logLevel.MinimumLevel);
            }
        }

        public static void LogError(string fmt, params object[] args)
        {
            logger.Error(fmt, args);
        }

        public static void LogWarning(string fmt, params object[] args)
        {
            logger.Warning(fmt, args);
        }

        public static void LogVerbose(string fmt, params object[] args)
        {
            logger.Verbose(fmt, args);
        }

        public static void LogTrace(string fmt, params object[] args)
        {
            logger.Debug(fmt, args);
        }

        public static void Log(string fmt, params object[] args)
        {
            logger.Information(fmt, args);
        }
    }
}
