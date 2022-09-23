using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Damselfly.Core.Services;

/// <summary>
///     Service to generate download files for exporting images from the system. Zip files
///     are built from the basket or other selection sets, and then created on disk in the
///     wwwroot folder. We can then pass them back to the browser as a URL to trigger a
///     download. The service can also perform transforms on the images before they're
///     zipped for download, such as resizing, rotations, watermarking etc.
/// </summary>
public class DownloadService : IDownloadService
{
    private const string s_appVPath = "desktop";
    private const string s_downloadVPath = "downloads";
    private const string s_completionMsg = "Zip created.";
    private static DirectoryInfo desktopPath;
    private static DirectoryInfo downloadsPath;
    private readonly IImageCacheService _cacheService;
    private readonly DesktopAppPaths _desktopAppInfo = new();
    private readonly ImageProcessService _imageProcessingService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStatusService _statusService;

    public DownloadService(IStatusService statusService, ImageProcessService imageService,
        IImageCacheService cacheService, IWebHostEnvironment env, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _statusService = statusService;
        _imageProcessingService = imageService;
        _cacheService = cacheService;

        SetDownloadPath(env.WebRootPath);
    }

    public async Task<DesktopAppPaths> GetDesktopAppInfo()
    {
        return await Task.FromResult(_desktopAppInfo);
    }

    /// <summary>
    ///     Download a collection of images.
    /// </summary>
    /// <param name="imagesToZip"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public async Task<string> CreateDownloadZipAsync(ICollection<int> imagesIdsToZip, ExportConfig config)
    {
        var images = await _cacheService.GetCachedImages(imagesIdsToZip);

        var imagePaths = images.Select(x => new FileInfo(x.FullPath)).ToArray();

        return await CreateDownloadZipAsync(imagePaths, config);
    }

    /// <summary>
    ///     Initialise the service with the download file path - which will usually
    ///     be a subfolder of the wwwroot content folder.
    /// </summary>
    /// <param name="contentRootPath"></param>
    public void SetDownloadPath(string contentRootPath)
    {
        desktopPath = new DirectoryInfo(Path.Combine(contentRootPath, s_appVPath));
        downloadsPath = new DirectoryInfo(Path.Combine(contentRootPath, s_downloadVPath));

        if ( !downloadsPath.Exists )
        {
            downloadsPath.Create();
            Logging.Log($"Created downloads folder: {downloadsPath}");
        }
        else
        {
            Logging.Log($"Downloads folder: {downloadsPath}");
        }

        if ( desktopPath.Exists )
            // Now, see if we have a desktop app
            CheckDesktopAppPaths(desktopPath);
    }

    /// <summary>
    ///     Get the paths of the various desktop apps
    /// </summary>
    /// <param name="desktopPath"></param>
    /// TODO: We should inject the json config that the app uses,
    /// with the endpoint pre-configured, into the zip here.
    private void CheckDesktopAppPaths(DirectoryInfo desktopPath)
    {
        // Get the files in the desktop folder
        var desktopFiles = desktopPath.GetFiles("*.*")
            .Where(x => x.Name.StartsWith("Damselfly-", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Check for universal first; if not, use the normal mac version.
        var macAppPath =
            desktopFiles.FirstOrDefault(x => x.Name.EndsWith("-mac-universal.zip", StringComparison.OrdinalIgnoreCase));

        if ( macAppPath != null )
        {
            _desktopAppInfo.MacOSApp = Path.Combine(s_appVPath, macAppPath.Name);
        }
        else
        {
            // No universal, so check for the Intel Mac version
            macAppPath =
                desktopFiles.FirstOrDefault(x => x.Name.EndsWith("-mac.zip", StringComparison.OrdinalIgnoreCase));

            if ( macAppPath != null )
                _desktopAppInfo.MacOSApp = Path.Combine(s_appVPath, macAppPath.Name);

            // We only care about the M1 version if the unversal isn't available.
            var m1AppPath = desktopFiles.FirstOrDefault(x =>
                x.Name.EndsWith("-mac-arm64.zip", StringComparison.OrdinalIgnoreCase));

            if ( m1AppPath != null )
                _desktopAppInfo.MacOSArmApp = Path.Combine(s_appVPath, m1AppPath.Name);
        }

        var winAppPath =
            desktopFiles.FirstOrDefault(x => x.Name.EndsWith("-win.zip", StringComparison.OrdinalIgnoreCase));

        if ( winAppPath != null )
            _desktopAppInfo.WindowsApp = Path.Combine(s_appVPath, winAppPath.Name);

        var linuxAppPath =
            desktopFiles.FirstOrDefault(x => x.Name.EndsWith(".appimage", StringComparison.OrdinalIgnoreCase));

        if ( linuxAppPath != null )
            _desktopAppInfo.LinuxApp = Path.Combine(s_appVPath, linuxAppPath.Name);
    }

    /// <summary>
    ///     Async method to create a download zip file, given a set of files on disk. Optionally
    ///     pass a watermark to stamp all images as they're written into the zip file.
    /// </summary>
    /// <param name="filesToZip">Array of files to zip</param>
    /// <param name="waterMarkText">Watermark text to apply to each image</param>
    /// <param name="keepPaths">
    ///     If true, keeps the images in subfolders to match their disk structure.
    ///     Otherwise, the contents are a flat list of images.
    /// </param>
    /// <param name="OnProgress">Callback to report progress.</param>
    /// <returns></returns>
    // TODO: If only one file selected, download directly instead of zipping
    public async Task<string> CreateDownloadZipAsync(FileInfo[] filesToZip, ExportConfig config)
    {
        Logging.Log($"Preparing zip file from {filesToZip.Length} files.");

        // The actual zip filename
        var zipfileName = $"DamselflyImages-{DateTime.UtcNow:dd-MMM-yy_hh_mm_ss}.zip";
        // The http path the file
        var virtualZipPath = Path.Combine(s_downloadVPath, zipfileName);
        // The local server filesystem path
        var serverZipPath = Path.Combine(downloadsPath.FullName, zipfileName);

        try
        {
            var zipFolder = Path.GetDirectoryName(serverZipPath);

            if ( !Directory.Exists(zipFolder) )
                Directory.CreateDirectory(zipFolder);

            if ( File.Exists(serverZipPath) )
                File.Delete(serverZipPath);

            Logging.Log($" Opening zip archive: {serverZipPath}");
            _statusService.UpdateStatus($"Preparing to zip {filesToZip.Count()} images...");

            using ( var zip = ZipFile.Open(serverZipPath, ZipArchiveMode.Create) )
            {
                int count = 1, total = filesToZip.Count();

                foreach ( var imagePath in filesToZip )
                {
                    if ( imagePath.Exists )
                    {
                        Logging.Log($" Adding file to zip: {imagePath}");

                        var exportUnchanged = true;
                        var internalZipPath = imagePath.Name;

                        // If we're altering the file at all, postfix the name with _export
                        if ( !string.IsNullOrEmpty(config.WatermarkText) || config.Size != ExportSize.FullRes )
                        {
                            internalZipPath = Path.GetFileNameWithoutExtension(imagePath.Name) + "_export" +
                                              imagePath.Extension;
                            exportUnchanged = false;
                        }

                        if ( config.KeepFolders )
                            internalZipPath = Path.Combine(imagePath.Directory.Name, internalZipPath);

                        ZipArchiveEntry entry;

                        if ( exportUnchanged )
                        {
                            // Export the original file, as-is
                            entry = zip.CreateEntryFromFile(imagePath.FullName, internalZipPath);
                        }
                        else
                        {
                            // Transform the input file with rotation, watermark, etc.
                            entry = zip.CreateEntry(internalZipPath);

                            using ( var zipStream = entry.Open() )
                            {
                                // Run the transform - note we do this in-memory and directly on the stream so the
                                // transformed file is never actually written to disk other than in the zip.
                                await _imageProcessingService.TransformDownloadImage(imagePath.FullName, zipStream,
                                    config);
                            }
                        }

                        // Linux memory stream zip entries don't get good permissions, so fix that here:
                        // https://github.com/dotnet/runtime/issues/17912
                        // This also applies to files which don't have valid permissions on the current 
                        // file system, but can be read by the app as root.
                        // https://github.com/dotnet/runtime/issues/76006
                        entry.ExternalAttributes = entry.ExternalAttributes | ( Convert.ToInt32( "664", 8 ) << 16 );
                    }
                    else
                    {
                        Logging.LogWarning($"Zipped Image not found on disk: {imagePath}");
                    }

                    var percentComplete = count++ * 100 / total;

                    _statusService.UpdateStatus($"Zipping image {imagePath.Name}... ({percentComplete}% complete)");
                }

                _statusService.UpdateStatus(s_completionMsg);
            }

            return Path.DirectorySeparatorChar + virtualZipPath;
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception while creating zip file {serverZipPath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Tidy up the old downloads folder by deleting zip files which are older
    ///     than a certain age. This will be called periodically as a scheduled
    ///     task.
    /// </summary>
    /// <param name="timeSpan"></param>
    public void CleanUpOldDownloads(TimeSpan timeSpan)
    {
        if ( downloadsPath.Exists )
        {
            var threshold = DateTime.UtcNow - timeSpan;

            // Look for files eligible to clean up - they must have been created
            // before the last cleanup was scheduled.
            var toDelete = downloadsPath.GetFiles("*.*", SearchOption.AllDirectories)
                .Where(x => !x.Name.StartsWith('.'))
                .Where(x => x.CreationTimeUtc < threshold)
                .ToList();

            if ( toDelete.Any() )
            {
                Logging.LogWarning($"Cleaning up {toDelete.Count} download zips older than {threshold}");

                toDelete.ForEach(x => x.SafeDelete());
            }
        }
        else
        {
            Logging.LogWarning($"Downloads path ({downloadsPath}) did not exist.");
        }
    }

    /// <summary>
    ///     Saves an export config to the DB. Will update the config
    ///     if an entry with that name already exists
    /// </summary>
    /// <param name="config"></param>
    public async Task SaveDownloadConfig(ExportConfig config)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var existing = db.DownloadConfigs.SingleOrDefault(x => x.Name.Equals(config.Name));

        if ( existing != null )
        {
            config.ExportConfigId = existing.ExportConfigId;
            db.DownloadConfigs.Update(config);
        }
        else
        {
            db.DownloadConfigs.Add(config);
        }

        await db.SaveChangesAsync("SaveExportConfig");
    }
}