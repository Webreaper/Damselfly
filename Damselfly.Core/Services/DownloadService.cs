using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Models;
using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Interfaces;
using System.Threading;

namespace Damselfly.Core.Services
{
    public enum ExportType
    {
        Download = 1,
        Email = 2,
        Wordpress = 3,
        Facebook = 4,
        Twitter = 5,
        Instagram = 6,
    };

    public enum ExportSize
    {
        FullRes = 1,
        Large = 2,
        Medium = 3,
        Small = 4,
    };

    /// <summary>
    /// Service to generate download files for exporting images from the system. Zip files
    /// are built from the basket or other selection sets, and then created on disk in the
    /// wwwroot folder. We can then pass them back to the browser as a URL to trigger a 
    /// download. The service can also perform transforms on the images before they're
    /// zipped for download, such as resizing, rotations, watermarking etc.
    /// </summary>
    public class DownloadService
    {
        public class DesktopAppPaths
        {
            public string MacOSApp { get; set; }
            public string MacOSArmApp { get; set; }
            public string WindowsApp { get; set; }
            public string LinuxApp { get; set; }
        }

        public static DownloadService Instance { get; private set; }
        public static DesktopAppPaths DesktopAppInfo { get; private set; } = new DesktopAppPaths();
        private static DirectoryInfo desktopPath;
        private static DirectoryInfo downloadsPath;
        private const string s_appVPath = "desktop";
        private const string s_downloadVPath = "downloads";
        private const string s_completionMsg = "Zip created.";

        public DownloadService()
        {
            Instance = this;
        }

        /// <summary>
        /// Initialise the service with the download file path - which will usually
        /// be a subfolder of the wwwroot content folder.
        /// </summary>
        /// <param name="contentRootPath"></param>
        public void SetDownloadPath( string contentRootPath )
        {
            desktopPath = new DirectoryInfo(Path.Combine(contentRootPath, s_appVPath));
            downloadsPath = new DirectoryInfo( Path.Combine(contentRootPath, s_downloadVPath) );

            if (!downloadsPath.Exists)
            {
                downloadsPath.Create();
                Logging.Log($"Created downloads folder: {downloadsPath}");
            }
            else
                Logging.Log($"Downloads folder: {downloadsPath}");

            // Now, see if we have a desktop app

            // Get the files in the desktop folder
            var desktopFiles = desktopPath.GetFiles("*.*")
                                          .Where( x => x.Name.StartsWith("Damselfly-", StringComparison.OrdinalIgnoreCase) )
                                          .ToList();

            // TODO: We should inject the json config that the app uses,
            // with the endpoint pre-configured, into the zip here.
            var macAppPath = desktopFiles.FirstOrDefault( x => x.Name.EndsWith( "-mac.zip", StringComparison.OrdinalIgnoreCase ) );

            if (macAppPath != null)
                DesktopAppInfo.MacOSApp = Path.Combine(s_appVPath, macAppPath.Name);

            var m1AppPath = desktopFiles.FirstOrDefault(x => x.Name.EndsWith("-mac-arm64.zip", StringComparison.OrdinalIgnoreCase));

            if (m1AppPath != null)
                DesktopAppInfo.MacOSArmApp = Path.Combine(s_appVPath, m1AppPath.Name);

            var winAppPath = desktopFiles.FirstOrDefault(x => x.Name.EndsWith("-win.zip", StringComparison.OrdinalIgnoreCase));

            if (winAppPath != null)
                DesktopAppInfo.WindowsApp = Path.Combine(s_appVPath, winAppPath.Name);

            var linuxAppPath = desktopFiles.FirstOrDefault(x => x.Name.EndsWith(".appimage", StringComparison.OrdinalIgnoreCase));

            if (linuxAppPath != null)
                DesktopAppInfo.LinuxApp = Path.Combine(s_appVPath, linuxAppPath.Name);
        }

        /// <summary>
        /// Async method to create a download zip file, given a set of files on disk. Optionally
        /// pass a watermark to stamp all images as they're written into the zip file. 
        /// </summary>
        /// <param name="filesToZip">Array of files to zip</param>
        /// <param name="waterMarkText">Watermark text to apply to each image</param>
        /// <param name="keepPaths">If true, keeps the images in subfolders to match their disk structure.
        /// Otherwise, the contents are a flat list of images.</param>
        /// <param name="OnProgress">Callback to report progress.</param>
        /// <returns></returns>
        // TODO: If only one file selected, download directly instead of zipping
        public async Task<string> CreateDownloadZipAsync( FileInfo[] filesToZip, ExportConfig config, bool keepPaths )
        {
            Logging.Log($"Preparing zip file from {filesToZip.Length} files.");

            // The actual zip filename
            string zipfileName = $"DamselflyImages-{DateTime.UtcNow:dd-MMM-yy_hh_mm_ss}.zip";
            // The http path the file
            string virtualZipPath = Path.Combine(s_downloadVPath, zipfileName);
            // The local server filesystem path
            string serverZipPath = Path.Combine(downloadsPath.FullName, zipfileName);

            try
            {
                string zipFolder = Path.GetDirectoryName(serverZipPath);

                if (!Directory.Exists(zipFolder))
                    Directory.CreateDirectory(zipFolder);

                if (File.Exists(serverZipPath))
                    File.Delete(serverZipPath);

                Logging.Log($" Opening zip archive: {serverZipPath}");
                StatusService.Instance.StatusText = $"Preparing to zip {filesToZip.Count()} images...";

                using (ZipArchive zip = ZipFile.Open(serverZipPath, ZipArchiveMode.Create))
                {
                    int count = 1, total = filesToZip.Count();

                    foreach (var imagePath in filesToZip)
                    {
                        if (imagePath.Exists)
                        {
                            Logging.Log($" Adding file to zip: {imagePath}");

                            bool exportUnchanged = true;
                            string internalZipPath = imagePath.Name;

                            // If we're altering the file at all, postfix the name with _export
                            if (! String.IsNullOrEmpty(config.WatermarkText) || config.Size != ExportSize.FullRes )
                            {
                                internalZipPath = Path.GetFileNameWithoutExtension(imagePath.Name) + "_export" + imagePath.Extension;
                                exportUnchanged = false;
                            }

                            if (keepPaths)
                                internalZipPath = Path.Combine(imagePath.Directory.Name, internalZipPath);

                            if (exportUnchanged)
                            {
                                // Export the original file, as-is
                                zip.CreateEntryFromFile(imagePath.FullName, internalZipPath);
                            }
                            else
                            {
                                // Transform the input file with rotation, watermark, etc.
                                var file = zip.CreateEntry(internalZipPath);

                                using (var zipStream = file.Open())
                                {
                                    // Run the transform - note we do this in-memory and directly on the stream so the
                                    // transformed file is never actually written to disk other than in the zip.
                                    await Task.Run(() => ImageProcessService.Instance.TransformDownloadImage(imagePath.FullName,
                                                            zipStream, config.WatermarkText ));
                                }
                            }
                        }
                        else
                            Logging.LogWarning($"Zipped Image not found on disk: {imagePath}");

                        int percentComplete = (count++ * 100) / total;

                        // Yield a bit, otherwise 
                        await Task.Delay(50);

                        StatusService.Instance.StatusText = $"Zipping image {imagePath.Name}... ({percentComplete}% complete)";
                    }

                    StatusService.Instance.StatusText = s_completionMsg;
                }

                return virtualZipPath;
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception while creating zip file {serverZipPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tidy up the old downloads folder by deleting zip files which are older
        /// than a certain age. This will be called periodically as a scheduled
        /// task.
        /// </summary>
        /// <param name="timeSpan"></param>
        public void CleanUpOldDownloads( TimeSpan timeSpan)
        {
            if (downloadsPath.Exists)
            {
                var threshold = DateTime.UtcNow - timeSpan;

                Logging.LogWarning($"Cleaning up download zips older than {threshold}");

                // Look for files eligible to clean up - they must have been created
                // before the last cleanup was scheduled.
                var toDelete = downloadsPath.GetFiles("*.*", SearchOption.AllDirectories)
                                         .Where(x => !x.Name.StartsWith('.'))
                                         .Where(x => x.CreationTimeUtc < threshold)
                                         .ToList();

                toDelete.ForEach(x => x.SafeDelete());
            }
            else
                Logging.LogWarning($"Downloads path ({downloadsPath}) did not exist.");
        }

        /// <summary>
        /// Saves an export config to the DB. Will update the config
        /// if an entry with that name already exists
        /// </summary>
        /// <param name="config"></param>
        public async Task SaveDownloadConfig( ExportConfig config )
        {
            using var db = new ImageContext();

            var existing = db.DownloadConfigs.SingleOrDefault(x => x.Name.Equals(config.Name));

            if (existing != null)
            {
                config.ExportConfigId = existing.ExportConfigId;
                db.DownloadConfigs.Update(config);
            }
            else
            {
                db.DownloadConfigs.Add(config);
            }

            await Task.FromResult( db.SaveChanges("SaveExportConfig") );
        }
    }
}
