using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Images;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stopwatch = Damselfly.Shared.Utils.Stopwatch;

namespace Damselfly.Core.Services;

public class ThumbnailService : IProcessJobFactory, IRescanProvider
{
    private const string _requestRoot = "/images";
    private static string _thumbnailRootFolder;
    private static readonly int s_maxThreads = GetMaxThreads();


    /// <summary>
    ///     This is the set of thumb resolutions that Syno PhotoStation and moments expects
    /// </summary>
    private static readonly IThumbConfig[] thumbConfigs =
    {
        new ThumbConfig
            { width = 2000, height = 2000, size = ThumbSize.ExtraLarge, useAsSource = true, batchGenerate = false },
        new ThumbConfig { width = 800, height = 800, size = ThumbSize.Large, useAsSource = true },
        new ThumbConfig { width = 640, height = 640, size = ThumbSize.Big, batchGenerate = false },
        new ThumbConfig { width = 320, height = 320, size = ThumbSize.Medium },
        new ThumbConfig
            { width = 160, height = 120, size = ThumbSize.Preview, cropToRatio = true, batchGenerate = false },
        new ThumbConfig { width = 120, height = 120, size = ThumbSize.Small, cropToRatio = true }
    };

    private readonly ImageCache _imageCache;
    private readonly ImageProcessService _imageProcessingService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfigService _configService;
    // private readonly WorkService _workService;
    private readonly ImageContext db;

    public ThumbnailService(IServiceScopeFactory scopeFactory,
        ImageProcessService imageService,
        IConfigService configService,
        ImageCache imageCache, 
        // WorkService workService,
        IConfiguration configuration,
        ImageContext imageContext)
    {
        _scopeFactory = scopeFactory;
        _imageProcessingService = imageService;
        _imageCache = imageCache;
        // _workService = workService;
        _configService = configService;
        PicturesRoot = configuration["DamselflyConfiguration:SourceDirectory"]!;
        Synology = configuration["DamselflyConfiguration:Synology"] == "true";
        SetThumbnailRoot(configuration["DamselflyConfiguration:ThumbPath"]!);
        // _workService.AddJobSource(this);
        db = imageContext;
    }

    public static string PicturesRoot { get; set; }
    public static bool UseGraphicsMagick { get; set; }
    public static bool Synology { get; set; }
    public static string RequestRoot => _requestRoot;

    public JobPriorities Priority => JobPriorities.Thumbnails;

    private bool BackgroundThumbnailProcessingEnabled
    {
        get
        {
            return _configService.GetBool( ConfigSettings.EnableBackgroundThumbs, false );
        }
    }

    public async Task<ICollection<IProcessJob>> GetPendingJobs(int maxJobs)
    {
        if ( !BackgroundThumbnailProcessingEnabled )
            return new ThumbProcess[0];

        using var scope = _scopeFactory.CreateScope();

        var images = await db!.ImageMetaData.Where(x => x.ThumbLastUpdated == null)
            .OrderByDescending(x => x.LastUpdated)
            .Take(maxJobs)
            .Select(x => x.ImageId)
            .ToListAsync();

        var jobs = images.Select(x => new ThumbProcess { ImageId = x, Service = this })
            .ToArray();

        return jobs;
    }

    private void DeleteThumbnails( IEnumerable<FileInfo> imagePaths )
    {
        var sizes = thumbConfigs.Select( x => x.size ).ToList();

        var thumbPaths = imagePaths.SelectMany( x => sizes.Select( sz => GetThumbPath( x, sz ) ) )
                            .Select( x => new FileInfo( x ) )
                            .ToList();

        foreach( var thumb in thumbPaths )
        {
            thumb.SafeDelete();
        }
    }

    private async Task DeleteFolderThumbnails( Guid folderId )
    {
        using var scope = _scopeFactory.CreateScope();

        var files = await db!.Images.Where( x => x.FolderId == folderId )
                             .Include( x => x.Folder )
                             .Select( x => new FileInfo( Path.Combine( x.Folder.Path, x.FileName ) ) )
                             .ToListAsync();

        DeleteThumbnails( files );
    }

    private async Task DeleteThumbnails( IEnumerable<Guid> imageIds)
    {
        using var scope = _scopeFactory.CreateScope();

        var files = await db!.Images.Where( x => imageIds.Contains( x.ImageId ) )
                             .Include( x => x.Folder )
                             .Select( x => new FileInfo( Path.Combine( x.Folder.Path, x.FileName ) ))
                             .ToListAsync();

        DeleteThumbnails( files );
    }

    public async Task MarkAllForScan()
    {
        using var scope = _scopeFactory.CreateScope();

        // TODO: Abstract this once EFCore Bulkextensions work in efcore 6
        var updated =
            await db.Database.ExecuteSqlInterpolatedAsync($"Update imagemetadata Set ThumbLastUpdated = null");
    }

    public async Task MarkFolderForScan(Guid folderId)
    {
        using var scope = _scopeFactory.CreateScope();

        DateTime? lastUpdateDate = BackgroundThumbnailProcessingEnabled ? DateTime.UtcNow : null;

        var updated = await ImageContext.UpdateMetadataFields(db, folderId, "ThumbLastUpdated", 
                                                $"'{lastUpdateDate:dd/MM/yyyy HH:mm:ss}'");

        if( !BackgroundThumbnailProcessingEnabled )
            await DeleteFolderThumbnails( folderId );

    }

    public async Task MarkImagesForScan(ICollection<Guid> imageIds)
    {
        using var scope = _scopeFactory.CreateScope();
        
        // If we're not background generating thumbs, set the last updated to right now
        // which will force us to re-request the thumb and it'll be generated by the 
        // controller.
        DateTime? lastUpdateDate = BackgroundThumbnailProcessingEnabled ? null : DateTime.UtcNow;
        
        await db.BatchUpdate(db.ImageMetaData, i => 
            i.SetProperty(x => x.ThumbLastUpdated, 
                x => lastUpdateDate));
        
        if( !BackgroundThumbnailProcessingEnabled )
        {
            // Delete the thumbs on disk so they'll be regenerated when next requested
            await DeleteThumbnails( imageIds );
            _imageCache.Evict(imageIds.ToList());
        }

        var msgText = imageIds.Count == 1 ? "Image" : $"{imageIds.Count} images";
        // _workService.FlagNewJobs(this);
    }

    /// <summary>
    ///     TODO - move this somewhere better
    /// </summary>
    /// <returns></returns>
    public static int GetMaxThreads()
    {
        if ( Debugger.IsAttached )
            return 1;

        return Math.Max(Environment.ProcessorCount / 2, 2);
    }

    /// <summary>
    ///     Set the http thumbnail request root - this will be wwwroot or equivalent
    ///     and will be determined by the webserver we're being called from.
    /// </summary>
    /// <param name="rootFolder"></param>
    public static void SetThumbnailRoot(string rootFolder)
    {
        if( _thumbnailRootFolder != null ) return;
        // Get the full absolute path.
        _thumbnailRootFolder = Path.GetFullPath(rootFolder);

        if ( !Synology )
        {
            if ( !Directory.Exists(_thumbnailRootFolder) )
            {
                Directory.CreateDirectory(_thumbnailRootFolder);
                Logging.Log("Created folder for thumbnails storage at {0}", _thumbnailRootFolder);
            }
            else
            {
                Logging.Log("Initialised thumbnails storage at {0}", _thumbnailRootFolder);
            }
        }
    }

    /// <summary>
    ///     Given a particular image, calculates the path and filename of the associated
    ///     thumbnail for that image and size.
    ///     TODO: Use the Thumbnail Last gen date here to avoid passing back images with no thumbs?
    /// </summary>
    /// <param name="imageFile"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public string GetThumbPath(FileInfo imageFile, ThumbSize size)
    {
        string thumbPath;

        if ( Synology )
        {
            // Syno thumbs go in a subdir of the location of the image
            var thumbFileName = $"SYNOPHOTO_THUMB_{GetSizePostFix(size).ToUpper()}.jpg";
            thumbPath = Path.Combine(imageFile.DirectoryName, "@eaDir", imageFile.Name, thumbFileName);
        }
        else
        {
            var extension = imageFile.Extension;

            // Keep the extension if it's JPG, but otherwise change it to JPG (for HEIC etc).
            if ( !extension.Equals(".JPG", StringComparison.OrdinalIgnoreCase) )
                extension = ".JPG";

            var baseName = Path.GetFileNameWithoutExtension(imageFile.Name);
            var relativePath = imageFile.DirectoryName.MakePathRelativeTo(PicturesRoot);
            var thumbFileName = $"{baseName}_{GetSizePostFix(size)}{extension}";
            thumbPath = Path.Combine(_thumbnailRootFolder, relativePath, thumbFileName);
        }

        return thumbPath;
    }

    private string GetSizePostFix(ThumbSize size)
    {
        return size switch
        {
            ThumbSize.ExtraLarge => "xl",
            ThumbSize.Large => "l",
            ThumbSize.Big => "b",
            ThumbSize.Medium => "m",
            ThumbSize.Small => "s",
            _ => "PREVIEW"
        };
    }

    /// <summary>
    ///     Gets the list of thumbnails sizes/specs to generate
    /// </summary>
    /// <param name="source"></param>
    /// <param name="ignoreExisting">Force the creation even if there's an existing file with the correct timestamp</param>
    /// <param name="altSource">If an existing thumbnail can be used as a source image, returns it</param>
    /// <returns></returns>
    private Dictionary<FileInfo, IThumbConfig> GetThumbConfigs(FileInfo source, bool forceRegeneration,
        out FileInfo altSource)
    {
        altSource = null;

        var thumbFileAndConfig = new Dictionary<FileInfo, IThumbConfig>();

        // First pre-check whether the thumbs exist
        foreach ( var thumbConfig in thumbConfigs.Where(x => x.batchGenerate) )
        {
            var destFile = new FileInfo(GetThumbPath(source, thumbConfig.size));

            if ( !destFile.Directory.Exists )
            {
                Logging.LogTrace("Creating directory: {0}", destFile.Directory.FullName);
                var newDir = Directory.CreateDirectory(destFile.Directory.FullName);
            }

            var needToGenerate = true;

            if( destFile.Exists )
            {
                // We have a thumbnail on disk. See if it's suitable,
                // or if it needs to be regenerated.
                if( !forceRegeneration )
                {
                    // First, check if the source is older than the thumbnail
                    if( source.LastWriteTimeUtc < destFile.LastWriteTimeUtc )
                    {
                        // The source is older, so we might be able to use it. Check the res:
                        int actualHeight, actualWidth;
                        MetaDataService.GetImageSize( destFile.FullName, out actualWidth, out actualHeight );

                        // Note that the size may be smaller - thumbconfigs are 'max' size, not actual.
                        if( actualHeight <= thumbConfig.height && actualWidth <= thumbConfig.width )
                        {
                            // Size matches - so no need to generate.
                            needToGenerate = false;

                            // If the creation time of both files is the same, we're done.
                            Logging.LogTrace( "File {0} already exists with matching creation time.", destFile );

                            // Since a smaller version that's suitable as a source exists, use it. This is a
                            // performance enhancement - it means that if we're scaling a 7MB image, but a 1MB
                            // thumbnail already exists, use that as the source instead, as it'll be faster
                            // to process.
                            if( altSource == null && thumbConfig.useAsSource )
                                altSource = destFile;
                        }
                    }
                }
            }

            if ( needToGenerate ) 
                thumbFileAndConfig.Add(destFile, thumbConfig);
        }

        return thumbFileAndConfig;
    }

    /// <summary>
    ///     Go through all of the thumbnails and delete any thumbs that
    ///     don't apply to a legit iamage.
    /// </summary>
    /// <param name="thumbCleanupFreq"></param>
    public void CleanUpThumbnails(TimeSpan thumbCleanupFreq)
    {
        try
        {
            Logging.Log($"Clean up thumb started");
            var root = new DirectoryInfo(PicturesRoot);
            var thumbRoot = new DirectoryInfo(_thumbnailRootFolder);

            CleanUpThumbDir(root, thumbRoot);
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception during thumbnail cleanup: {ex}");
        }
    }

    private void CleanUpThumbDir(DirectoryInfo picsFolder, DirectoryInfo thumbsFolder)
    {
        // Check the images here.
        var thumbsToKeep = thumbConfigs.Where(x => x.batchGenerate);
        var picsSubDirs = picsFolder.SafeGetSubDirectories().Select(x => x.Name);
        var thumbSubDirs = thumbsFolder.SafeGetSubDirectories().Select(x => x.Name);

        var foldersToDelete = thumbSubDirs.Except(picsSubDirs);
        var foldersToCheck = thumbSubDirs.Intersect(picsSubDirs);

        foreach ( var deleteDir in foldersToDelete ) 
            Logging.Log($"Deleting folder {deleteDir} [Dry run]");

        foreach ( var folderToCheck in foldersToCheck.Select(x => new DirectoryInfo(x)) )
        {
            var allFiles = folderToCheck.GetFiles("*.*");
            var allThumbFiles =
                allFiles.SelectMany(file => thumbsToKeep.Select(thumb => GetThumbPath(file, thumb.size)));

            //var filesToDelete = allFiles;

            // Build hashmap of all base filenames without postfix or extension. Then enumerate
            // thumb files, and any that aren't found, delete
        }
    }

    /// <summary>
    ///     Queries the database to find any images that haven't had a thumbnail
    ///     generated, and queues them up to process the thumb generation.
    /// </summary>
    private async Task ProcessThumbnailScan()
    {
        using var scope = _scopeFactory.CreateScope();

        Logging.LogVerbose("Starting thumbnail scan...");

        var complete = false;

        while ( !complete )
        {
            Logging.LogVerbose("Querying DB for pending thumbs...");

            var watch = new Stopwatch("GetThumbnailQueue");

            // TODO: Change this to a consumer/producer thread model
            var imagesToScan = db.ImageMetaData.Where(x => x.ThumbLastUpdated == null)
                .OrderByDescending(x => x.LastUpdated)
                .Take(100)
                .Include(x => x.Image)
                .Include(x => x.Image.Hash)
                .Include(x => x.Image.Folder)
                .ToArray();

            watch.Stop();

            complete = !imagesToScan.Any();

            if ( !complete )
            {
                Logging.LogVerbose(
                    $"Found {imagesToScan.Count()} images requiring thumb gen. First image is {imagesToScan[0].Image.FullPath}.");

                watch = new Stopwatch("ThumbnailBatch", 100000);

                // We always ignore existing thumbs when generating
                // them based onthe ThumbLastUpdated date.
                const bool forceRegeneration = false;

                Logging.LogVerbose($"Executing CreatThumbs in parallel with {s_maxThreads} threads.");

                try
                {
                    await imagesToScan.ExecuteInParallel(async img => await CreateThumbs(img, forceRegeneration),
                        s_maxThreads);
                }
                catch ( Exception ex )
                {
                    Logging.LogError($"Exception during parallelised thumbnail generation: {ex}");
                }

                // Write the timestamps for the newly-generated thumbs.
                Logging.LogVerbose("Writing thumbnail generation timestamp updates to DB.");

                var updateWatch = new Stopwatch("BulkUpdateThumGenDate");
                await db.BulkUpdate(db.ImageMetaData, imagesToScan.ToList());
                updateWatch.Stop();

                watch.Stop();

                Action<string> logFunc = Logging.Verbose ? s => Logging.LogVerbose(s) : s => Logging.Log(s);
                Stopwatch.WriteTotals(logFunc);
            }
            else
            {
                Logging.LogVerbose("No images found to scan.");
            }
        }
    }

    /// <summary>
    ///     Generates thumbnails for an image.
    /// </summary>
    /// <param name="sourceImage"></param>
    /// <param name="forceRegeneration"></param>
    /// <returns></returns>
    public async Task<IImageProcessResult> CreateThumbs(ImageMetaData sourceImage, bool forceRegeneration)
    {
        // Mark the image as done, so that if anything goes wrong it won't go into an infinite loop spiral
        sourceImage.ThumbLastUpdated = DateTime.UtcNow;

        var result = await ConvertFile(sourceImage.Image, forceRegeneration);

        _imageCache.Evict(sourceImage.ImageId);

        return result;
    }

    /// <summary>
    ///     Generates thumbnails for an image.
    /// </summary>
    /// <param name="sourceImage"></param>
    /// <param name="forceRegeneration"></param>
    /// <returns></returns>
    public async Task<IImageProcessResult> CreateThumb(Guid imageId, ThumbSize size = ThumbSize.Unknown)
    {
        using var scope = _scopeFactory.CreateScope();

        var image = await db.Images.Include(i => i.MetaData).Include(i => i.Folder).FirstOrDefaultAsync(i => i.ImageId == imageId); // await _imageCache.GetCachedImage(imageId);

        // image = db.AttachToOrGet(x => x.ImageId == imageId , () => image);

        // Mark the image as done, so that if anything goes wrong it won't go into an infinite loop spiral
        // image.MetaData = db.AttachToOrGet(x => x.MetaDataId == image.MetaData.MetaDataId, () => image.MetaData);
        image.MetaData.ThumbLastUpdated = DateTime.UtcNow;

        var result = await ConvertFile(image, false, size);
        
        db.Images.Update(image);
        await db.SaveChangesAsync("UpdateThumbTimeStamp");

        _imageCache.Evict(image.ImageId);

        return result;
    }

    public async Task<IImageProcessResult> CreateThumb(Image image, ThumbSize size)
    {
        // image.Albums.Clear();
        if (image.MetaData == null)
        {
            image.MetaData = await db.ImageMetaData.FirstOrDefaultAsync(x => x.ImageId == image.ImageId);
        }
        image.MetaData.ThumbLastUpdated = DateTime.UtcNow;

        var result = await ConvertFile(image, false, size);
        
        db.ImageMetaData.Update(image.MetaData);
        await db.SaveChangesAsync("UpdateThumbTimeStamp");
        return result;
    }

    /// <summary>
    ///     Saves an MD5 Image hash against an image.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="processResult"></param>
    /// <returns></returns>
    public async Task AddHashToImage(Image image, IImageProcessResult processResult)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var hash = image.Hash;

            if ( hash == null )
            {
                hash = new Hash { ImageId = image.ImageId };
                image.Hash = hash;
                db.Hashes.Add(hash);
            }
            else
            {
                hash = db.AttachToOrGet(x => x.HashId == hash.HashId, () => hash);
                db.Hashes.Update(hash);
            }

            hash.MD5ImageHash = processResult.ImageHash;
            hash.SetFromHexString(processResult.PerceptualHash);

            await db.SaveChangesAsync("SaveHash");
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception during perceptual hash calc: {ex}");
        }
    }

    /// <summary>
    ///     Clears the cache of face thumbs from the disk
    /// </summary>
    public Task ClearFaceThumbs()
    {
        var dir = new DirectoryInfo(Path.Combine(_thumbnailRootFolder, "_FaceThumbs"));

        dir.GetFiles().ToList()
            .ForEach(x => x.SafeDelete());

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Given an image ID and a face object, returns the path of a generated
    ///     thumbnail for that croppped face.
    /// </summary>
    /// <param name="imageId"></param>
    /// <param name="face"></param>
    /// <returns></returns>
    //public async Task<FileInfo> GenerateFaceThumb(ImageObject face)
    //{
    //    FileInfo destFile = null;
    //    var watch = new Stopwatch("GenerateFaceThumb");

    //    try
    //    {
    //        var faceDir = Path.Combine(_thumbnailRootFolder, "_FaceThumbs");
    //        var image = await _imageCache.GetCachedImage(face.ImageId);
    //        var file = new FileInfo(image.FullPath);
    //        var thumbPath = new FileInfo(GetThumbPath(file, ThumbSize.Large));

    //        if ( thumbPath.Exists )
    //        {
    //            destFile = new FileInfo($"{faceDir}/face_{face.PersonId}.jpg");

    //            if ( !Directory.Exists(faceDir) )
    //            {
    //                Logging.Log($"Created folder for face thumbnails: {faceDir}");
    //                Directory.CreateDirectory(faceDir);
    //            }

    //            if ( !destFile.Exists )
    //            {
    //                Logging.Log($"Generating face thumb for {face.PersonId} from file {thumbPath}...");

    //                MetaDataService.GetImageSize(thumbPath.FullName, out var thumbWidth, out var thumbHeight);

    //                Logging.LogTrace($"Loaded {thumbPath.FullName} - {thumbWidth} x {thumbHeight}");

    //                var (x, y, width, height) = ScaleDownRect(image.MetaData.Width, image.MetaData.Height,
    //                    thumbWidth, thumbHeight,
    //                    face.RectX, face.RectY, face.RectWidth, face.RectHeight);

    //                Logging.LogTrace($"Cropping face at {x}, {y}, w:{width}, h:{height}");

    //                // TODO: Update person LastUpdated here?

    //                await _imageProcessingService.GetCroppedFile(thumbPath, x, y, width, height, destFile);

    //                destFile.Refresh();

    //                if ( !destFile.Exists )
    //                    destFile = null;
    //            }
    //        }
    //        else
    //        {
    //            Logging.LogWarning($"Unable to generate face thumb from {thumbPath} - file does not exist.");
    //        }
    //    }
    //    catch ( Exception ex )
    //    {
    //        Logging.LogError($"Exception generating face thumb for image ID {face.ImageId}: {ex.Message}");
    //    }

    //    watch.Stop();

    //    return destFile;
    //}

    /// <summary>
    ///     Scales the detected face/object rectangles based on the full-sized image,
    ///     since the object detection was done on a smaller thumbnail.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="imgObjects">Collection of objects to scale</param>
    /// <param name="thumbSize"></param>
    public static (int x, int y, int width, int height) ScaleDownRect(int imageWidth, int imageHeight, int sourceWidth,
        int sourceHeight, int x, int y, int width, int height)
    {
        if ( sourceHeight == 0 || sourceWidth == 0 )
            return (x, y, width, height);

        float longestBmpSide = sourceWidth > sourceHeight ? sourceWidth : sourceHeight;
        float longestImgSide = imageWidth > imageHeight ? imageWidth : imageHeight;

        var ratio = longestBmpSide / longestImgSide;

        var outX = (int)(x * ratio);
        var outY = (int)(y * ratio);
        var outWidth = (int)(width * ratio);
        var outHeight = (int)(height * ratio);

        var percentExpand = 0.3;
        var expandX = outWidth * percentExpand;
        var expandY = outHeight * percentExpand;

        outX = (int)Math.Max(outX - expandX, 0);
        outY = (int)Math.Max(outY - expandY, 0);

        outWidth = (int)(outWidth + expandX * 2);
        outHeight = (int)(outHeight + expandY * 2);

        if ( outX + outWidth > sourceWidth )
            outWidth = sourceWidth - outX;

        if ( outY + outHeight > sourceHeight )
            outHeight = outHeight - outY;

        return (outX, outY, outWidth, outHeight);
    }

    /// <summary>
    ///     Process the file on disk to create a set of thumbnails.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="forceRegeneration"></param>
    /// <returns></returns>
    public async Task<IImageProcessResult> ConvertFile(Image image, bool forceRegeneration,
        ThumbSize size = ThumbSize.Unknown)
    {
        var imagePath = new FileInfo(image.FullPath);
        IImageProcessResult? result = null;

        try
        {
            if ( imagePath.Exists )
            {
                Dictionary<FileInfo, IThumbConfig> destFiles;
                FileInfo? altSource = null;

                if ( size == ThumbSize.Unknown )
                {
                    // No explicit size passed, so we'll generate any that are flagged as batch-generate.
                    destFiles = GetThumbConfigs(imagePath, forceRegeneration, out altSource);
                }
                else
                {
                    var destFile = new FileInfo(GetThumbPath(imagePath, size));
                    var config = thumbConfigs.Where(x => x.size == size).FirstOrDefault();
                    destFiles = new Dictionary<FileInfo, IThumbConfig> { { destFile, config } };
                }

                if ( altSource != null )
                {
                    Logging.LogTrace("File {0} exists - using it as source for smaller thumbs.", altSource.Name);
                    imagePath = altSource;
                }

                // See if there's any conversions to do
                if ( destFiles.Any() )
                {
                    // First, pre-create the folders for any thumbs we'll be creating
                    destFiles.Select(x => x.Key.DirectoryName)
                        .Distinct().ToList()
                        .ForEach(dir => Directory.CreateDirectory(dir));

                    Logging.LogVerbose("Generating thumbnails for {0}", imagePath);

                    var watch = new Stopwatch("ConvertNative", 60000);
                    try
                    {
                        result = await _imageProcessingService.CreateThumbs(imagePath, destFiles);
                    }
                    catch ( Exception ex )
                    {
                        Logging.LogError("Thumbnail conversion failed for {0}: {1}", imagePath, ex.Message);
                    }
                    finally
                    {
                        watch.Stop();
                        Logging.LogVerbose(
                            $"{destFiles.Count()} thumbs created for {imagePath} in {watch.HumanElapsedTime}");
                    }

                    if ( result != null && result.ThumbsGenerated )
                    {
                        // Generate the perceptual hash from the large thumbnail.
                        var largeThumbPath = GetThumbPath(imagePath, ThumbSize.Large);

                        if ( File.Exists(largeThumbPath) )
                        {
                            result.PerceptualHash = _imageProcessingService.GetPerceptualHash(largeThumbPath);

                            // Store the hash with the image.
                            await AddHashToImage(image, result);
                        }
                    }
                }
                else
                {
                    Logging.LogVerbose("Thumbs already exist in all resolutions. Skipping...");
                    result = new ImageProcessResult { ThumbsGenerated = false };
                }
            }
            else
            {
                Logging.LogWarning("Skipping thumb generation for missing file...");
            }
        }
        catch ( Exception ex )
        {
            Logging.LogTrace("Exception converting thumbnails for {0}: {1}...", imagePath, ex.Message);
        }

        return result;
    }

    public class ThumbProcess : IProcessJob
    {
        public Guid ImageId { get; set; }
        public ThumbnailService Service { get; set; }
        public bool CanProcess => true;
        public string Name => "Thumbnail Generation";
        public string Description => $"Thumbnail gen for ID:{ImageId}";
        public JobPriorities Priority => JobPriorities.Thumbnails;

        public async Task Process()
        {
            await Service.CreateThumb(ImageId);
        }

        public override string ToString()
        {
            return Description;
        }
    }
}