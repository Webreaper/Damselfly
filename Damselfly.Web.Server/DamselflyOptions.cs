using System;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;

namespace Damselfly.Web.Server;


public class DamselflyOptions
{
    /// <summary>
    ///     Can't see this being used much around the web so let's
    ///     stake a claim for 6363 as our default port. :)
    /// </summary>
    private static readonly int s_defaultPort = 6363;

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
}
