using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils.Images;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.Services;

public class ThumbnailService : IProcessJobFactory
{
    private static string _thumbnailRootFolder;
    private const string _requestRoot = "/images";
    private static int s_maxThreads = GetMaxThreads();
    private readonly StatusService _statusService;
    private readonly ImageCache _imageCache;
    private readonly ImageProcessService _imageProcessingService;
    private readonly WorkService _workService;

    public ThumbnailService( StatusService statusService,
                    ImageProcessService imageService,
                    ImageCache imageCache, WorkService workService)
    {
        _statusService = statusService;
        _imageProcessingService = imageService;
        _imageCache = imageCache;
        _workService = workService;

        _workService.AddJobSource(this);
    }

    /// <summary>
    /// TODO - move this somewhere better
    /// </summary>
    /// <returns></returns>
    public static int GetMaxThreads()
    {
        if (System.Diagnostics.Debugger.IsAttached)
            return 1;

        return Math.Max(Environment.ProcessorCount / 2, 2);
    }

    public static string PicturesRoot { get; set; }
    public static bool UseGraphicsMagick { get; set; }
    public static bool Synology { get; set; }
    public static string RequestRoot { get { return _requestRoot; } }
    public static bool EnableThumbnailGeneration { get; set; } = true;

    /// <summary>
    /// Set the http thumbnail request root - this will be wwwroot or equivalent
    /// and will be determined by the webserver we're being called from.
    /// </summary>
    /// <param name="rootFolder"></param>
    public static void SetThumbnailRoot(string rootFolder)
    {
        // Get the full absolute path.
        _thumbnailRootFolder = Path.GetFullPath(rootFolder);

        if (!Synology)
        {
            if (!System.IO.Directory.Exists(_thumbnailRootFolder))
            {
                System.IO.Directory.CreateDirectory(_thumbnailRootFolder);
                Logging.Log("Created folder for thumbnails storage at {0}", _thumbnailRootFolder);
            }
            else
                Logging.Log("Initialised thumbnails storage at {0}", _thumbnailRootFolder);
        }
    }

    /// <summary>
    /// Given a particular image, calculates the path and filename of the associated
    /// thumbnail for that image and size.
    /// TODO: Use the Thumbnail Last gen date here to avoid passing back images with no thumbs?
    /// </summary>
    /// <param name="imageFile"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public string GetThumbPath(FileInfo imageFile, ThumbSize size)
    {
        string thumbPath;

        if (Synology)
        {
            // Syno thumbs go in a subdir of the location of the image
            string thumbFileName = $"SYNOPHOTO_THUMB_{GetSizePostFix(size).ToUpper()}.jpg";
            thumbPath = Path.Combine(imageFile.DirectoryName, "@eaDir", imageFile.Name, thumbFileName);
        }
        else
        {
            string extension = imageFile.Extension;

            // Keep the extension if it's JPG, but otherwise change it to JPG (for HEIC etc).
            if (!extension.Equals(".JPG", StringComparison.OrdinalIgnoreCase))
                extension = ".JPG";

            string baseName = Path.GetFileNameWithoutExtension(imageFile.Name);
            string relativePath = imageFile.DirectoryName.MakePathRelativeTo(PicturesRoot);  
            string thumbFileName = $"{baseName}_{GetSizePostFix(size)}{extension}";
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
            _ => "PREVIEW",
        };
    }


    /// <summary>
    /// This is the set of thumb resolutions that Syno PhotoStation and moments expects
    /// </summary>
    private static ThumbConfig[] thumbConfigs = {
        new ThumbConfig{ width = 2000, height = 2000, size = ThumbSize.ExtraLarge, useAsSource = true, batchGenerate = false},
        new ThumbConfig{ width = 800, height = 800, size = ThumbSize.Large, useAsSource = true },
        new ThumbConfig{ width = 640, height = 640, size = ThumbSize.Big, batchGenerate = false},
        new ThumbConfig{ width = 320, height = 320, size = ThumbSize.Medium },
        new ThumbConfig{ width = 160, height = 120, size = ThumbSize.Preview, cropToRatio = true, batchGenerate = false },
        new ThumbConfig{ width = 120, height = 120, size = ThumbSize.Small, cropToRatio = true }
    };

    /// <summary>
    /// Gets the list of thumbnails sizes/specs to generate
    /// </summary>
    /// <param name="source"></param>
    /// <param name="ignoreExisting">Force the creation even if there's an existing file with the correct timestamp</param>
    /// <param name="altSource">If an existing thumbnail can be used as a source image, returns it</param>
    /// <returns></returns>
    private Dictionary<FileInfo, ThumbConfig> GetThumbConfigs(FileInfo source, bool forceRegeneration, out FileInfo altSource)
    {
        altSource = null;

        var thumbFileAndConfig = new Dictionary<FileInfo, ThumbConfig>();

        // First pre-check whether the thumbs exist
        foreach ( var thumbConfig in thumbConfigs.Where( x => x.batchGenerate )  )
        {
            var destFile = new FileInfo( GetThumbPath(source, thumbConfig.size) );

            if ( ! destFile.Directory.Exists )
            {
                Logging.LogTrace("Creating directory: {0}", destFile.Directory.FullName);
                var newDir = System.IO.Directory.CreateDirectory( destFile.Directory.FullName );
            }

            bool needToGenerate = true;

            if( destFile.Exists )
            {
                // We have a thumbnail on disk. See if it's suitable,
                // or if it needs to be regenerated.
                if (!forceRegeneration)
                {
                    // First, check if the source is older than the thumbnail
                    if (source.LastWriteTimeUtc < destFile.LastWriteTimeUtc)
                    {
                        // The source is older, so we might be able to use it. Check the res:
                        int actualHeight, actualWidth;
                        MetaDataService.GetImageSize(destFile.FullName, out actualWidth, out actualHeight);

                        // Note that the size may be smaller - thumbconfigs are 'max' size, not actual.
                        if (actualHeight <= thumbConfig.height && actualWidth <= thumbConfig.width)
                        {
                            // Size matches - so no need to generate.
                            needToGenerate = false;

                            // If the creation time of both files is the same, we're done.
                            Logging.LogTrace("File {0} already exists with matching creation time.", destFile);

                            // Since a smaller version that's suitable as a source exists, use it. This is a
                            // performance enhancement - it means that if we're scaling a 7MB image, but a 1MB
                            // thumbnail already exists, use that as the source instead, as it'll be faster
                            // to process.
                            if (altSource == null && thumbConfig.useAsSource)
                                altSource = destFile;
                        }
                    }
                }
            }

            if( needToGenerate )
            {
                thumbFileAndConfig.Add(destFile, thumbConfig);
            }
        }

        return thumbFileAndConfig;
    }

    /// <summary>
    /// Go through all of the thumbnails and delete any thumbs that
    /// don't apply to a legit iamage.
    /// </summary>
    /// <param name="thumbCleanupFreq"></param>
    public void CleanUpThumbnails(TimeSpan thumbCleanupFreq)
    {
        DirectoryInfo root = new DirectoryInfo( PicturesRoot );
        DirectoryInfo thumbRoot = new DirectoryInfo(_thumbnailRootFolder);

        CleanUpThumbDir(root, thumbRoot);
    }

    private void CleanUpThumbDir( DirectoryInfo picsFolder, DirectoryInfo thumbsFolder )
    {
        // Check the images here.
        var thumbsToKeep = thumbConfigs.Where(x => x.batchGenerate);
        var picsSubDirs = picsFolder.SafeGetSubDirectories().Select(x => x.Name);
        var thumbSubDirs = thumbsFolder.SafeGetSubDirectories().Select(x => x.Name);

        var foldersToDelete = thumbSubDirs.Except(picsSubDirs);
        var foldersToCheck = thumbSubDirs.Intersect(picsSubDirs);

        foreach (var deleteDir in foldersToDelete)
        {
            Logging.Log($"Deleting folder {deleteDir} [Dry run]");
        }

        foreach (var folderToCheck in foldersToCheck.Select( x => new DirectoryInfo( x ) ) )
        {
            var allFiles = folderToCheck.GetFiles("*.*");
            var allThumbFiles = allFiles.SelectMany(file => thumbsToKeep.Select(thumb => GetThumbPath( file, thumb.size )));

            //var filesToDelete = allFiles;

            // Build hashmap of all base filenames without postfix or extension. Then enumerate
            // thumb files, and any that aren't found, delete
        }
    }

    /// <summary>
    /// Queries the database to find any images that haven't had a thumbnail
    /// generated, and queues them up to process the thumb generation.
    /// </summary>
    private async Task ProcessThumbnailScan()
    {
        using var db = new Models.ImageContext();

        Logging.LogVerbose("Starting thumbnail scan...");

        bool complete = false;

        while (!complete)
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

            if (!complete)
            {
                Logging.LogVerbose($"Found {imagesToScan.Count()} images requiring thumb gen. First image is {imagesToScan[0].Image.FullPath}.");

                watch = new Stopwatch("ThumbnailBatch", 100000);

                // We always ignore existing thumbs when generating
                // them based onthe ThumbLastUpdated date.
                const bool forceRegeneration = false;

                Logging.LogVerbose($"Executing CreatThumbs in parallel with {s_maxThreads} threads.");

                try
                {
                    await imagesToScan.ExecuteInParallel(async img => await CreateThumbs(img, forceRegeneration), s_maxThreads);
                }
                catch( Exception ex )
                {
                    Logging.LogError($"Exception during parallelised thumbnail generation: {ex}");
                }

                // Write the timestamps for the newly-generated thumbs.
                Logging.LogVerbose("Writing thumbnail generation timestamp updates to DB.");

                var updateWatch = new Stopwatch("BulkUpdateThumGenDate");
                await db.BulkUpdate( db.ImageMetaData, imagesToScan.ToList() );
                updateWatch.Stop();

                watch.Stop();

                if( imagesToScan.Length > 1 )
                    _statusService.StatusText = $"Completed thumbnail generation batch ({imagesToScan.Length} images in {watch.HumanElapsedTime}).";

                Stopwatch.WriteTotals();
            }
            else
                Logging.LogVerbose("No images found to scan.");
        }
    }

    /// <summary>
    /// Generates thumbnails for an image.
    /// </summary>
    /// <param name="sourceImage"></param>
    /// <param name="forceRegeneration"></param>
    /// <returns></returns>
    public async Task<ImageProcessResult> CreateThumbs(ImageMetaData sourceImage, bool forceRegeneration )
    {
        // Mark the image as done, so that if anything goes wrong it won't go into an infinite loop spiral
        sourceImage.ThumbLastUpdated = DateTime.UtcNow;

        var result = await ConvertFile(sourceImage.Image, forceRegeneration);

        _imageCache.Evict(sourceImage.ImageId);

        return result;
    }

    /// <summary>
    /// Generates thumbnails for an image.
    /// </summary>
    /// <param name="sourceImage"></param>
    /// <param name="forceRegeneration"></param>
    /// <returns></returns>
    public async Task<ImageProcessResult> CreateThumb(int imageId)
    {
        using var db = new ImageContext();

        var image = await _imageCache.GetCachedImage(imageId);

        // Mark the image as done, so that if anything goes wrong it won't go into an infinite loop spiral
        image.MetaData.ThumbLastUpdated = DateTime.UtcNow;

        var result = await ConvertFile(image, false);

        db.ImageMetaData.Update(image.MetaData);
        await db.SaveChangesAsync("UpdateThumbTimeStamp");

        _imageCache.Evict(image.ImageId);

        return result;
    }

    /// <summary>
    /// Saves an MD5 Image hash against an image. 
    /// </summary>
    /// <param name="image"></param>
    /// <param name="processResult"></param>
    /// <returns></returns>
    public async Task AddHashToImage( Image image, ImageProcessResult processResult )
    {
        try
        {
            using var db = new ImageContext();
            Hash hash = image.Hash;

            if (hash == null)
            {
                hash = new Hash { ImageId = image.ImageId };
                image.Hash = hash;
                db.Hashes.Add(hash);
            }
            else
            {
                db.Attach(hash);
                db.Hashes.Update(hash);
            }

            hash.MD5ImageHash = processResult.ImageHash;
            hash.PerceptualHash = processResult.PerceptualHash;

            await db.SaveChangesAsync("SaveHash");
        }
        catch( Exception ex )
        {
            Logging.LogError($"Exception during perceptual hash calc: {ex}");
        }
    }

    /// <summary>
    /// Clears the cache of face thumbs from the disk
    /// </summary>
    public void ClearFaceThumbs()
    {
        DirectoryInfo dir = new DirectoryInfo( Path.Combine(_thumbnailRootFolder, "_FaceThumbs") );

        dir.GetFiles().ToList()
                 .ForEach( x => FileUtils.SafeDelete(x));
    }

    /// <summary>
    /// Given an image ID and a face object, returns the path of a generated
    /// thumbnail for that croppped face.
    /// </summary>
    /// <param name="imageId"></param>
    /// <param name="face"></param>
    /// <returns></returns>
    public async Task<FileInfo> GenerateFaceThumb( ImageObject face )
    {
        FileInfo destFile = null;
        Stopwatch watch = new Stopwatch("GenerateFaceThumb");

        try
        {
            string faceDir = Path.Combine(_thumbnailRootFolder, "_FaceThumbs");
            var image = await _imageCache.GetCachedImage(face.ImageId);
            var file = new FileInfo(image.FullPath);
            var thumbPath = new FileInfo(GetThumbPath(file, ThumbSize.Large));

            if (thumbPath.Exists)
            {
                destFile = new FileInfo($"{faceDir}/face_{face.PersonId}.jpg");

                if (!System.IO.Directory.Exists(faceDir))
                {
                    Logging.Log($"Created folder for face thumbnails: {faceDir}");
                    System.IO.Directory.CreateDirectory(faceDir);
                }

                if (!destFile.Exists)
                {
                    Logging.Log($"Generating face thumb for {face.PersonId} from file {thumbPath}...");

                    MetaDataService.GetImageSize(thumbPath.FullName, out var thumbWidth, out var thumbHeight);

                    Logging.LogTrace($"Loaded {thumbPath.FullName} - {thumbWidth} x {thumbHeight}");

                    (var x, var y, var width, var height) = ScaleDownRect(image.MetaData.Width, image.MetaData.Height,
                                                                          thumbWidth, thumbHeight,
                                                                          face.RectX, face.RectY, face.RectWidth, face.RectHeight);

                    Logging.LogTrace($"Cropping face at {x}, {y}, w:{width}, h:{height}");

                    await _imageProcessingService.GetCroppedFile(thumbPath, x, y, width, height, destFile);

                    destFile.Refresh();

                    if (!destFile.Exists)
                        destFile = null;
                }
            }
            else
                Logging.LogWarning($"Unable to generate face thumb from {thumbPath} - file does not exist.");
        }
        catch( Exception ex )
        {
            Logging.LogError($"Exception generating face thumb for image ID {face.ImageId}: {ex.Message}");
        }

        watch.Stop();

        return destFile;
    }

    /// <summary>
    /// Scales the detected face/object rectangles based on the full-sized image,
    /// since the object detection was done on a smaller thumbnail.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="imgObjects">Collection of objects to scale</param>
    /// <param name="thumbSize"></param>
    public static (int x, int y, int width, int height) ScaleDownRect(int imageWidth, int imageHeight, int sourceWidth, int sourceHeight, int x, int y, int width, int height)
    {
        if (sourceHeight == 0 || sourceWidth == 0)
            return (x, y, width, height);

        float longestBmpSide = sourceWidth > sourceHeight ? sourceWidth : sourceHeight;
        float longestImgSide = imageWidth > imageHeight ? imageWidth : imageHeight;

        var ratio = (longestBmpSide / longestImgSide);

        int outX = (int)(x * ratio);
        int outY = (int)(y * ratio);
        int outWidth = (int)(width * ratio);
        int outHeight = (int)(height * ratio);

        double percentExpand = 0.3;
        double expandX = outWidth * percentExpand;
        double expandY = outHeight * percentExpand;

        outX = (int)Math.Max(outX - expandX, 0);
        outY = (int)Math.Max(outY - expandY, 0);

        outWidth = (int)(outWidth + (expandX * 2));
        outHeight = (int)(outHeight + (expandY * 2));

        if (outX + outWidth > sourceWidth)
            outWidth = sourceWidth - outX;

        if (outY + outHeight > sourceHeight)
            outHeight = outHeight - outY;

        return (outX, outY, outWidth, outHeight);
    }

    /// <summary>
    /// Process the file on disk to create a set of thumbnails.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="forceRegeneration"></param>
    /// <returns></returns>
    public async Task<ImageProcessResult> ConvertFile(Models.Image image, bool forceRegeneration, ThumbSize size = ThumbSize.Unknown )
    {
        var imagePath = new FileInfo(image.FullPath);
        ImageProcessResult result = null;

        try
        {
            if (imagePath.Exists)
            {
                Dictionary<FileInfo, ThumbConfig> destFiles;
                FileInfo altSource = null;

                if (size == ThumbSize.Unknown)
                {
                    // No explicit size passed, so we'll generate any that are flagged as batch-generate.
                    destFiles = GetThumbConfigs(imagePath, forceRegeneration, out altSource);
                }
                else
                {
                    var destFile = new FileInfo(GetThumbPath(imagePath, size));
                    var config = thumbConfigs.Where(x => x.size == size).FirstOrDefault();
                    destFiles = new Dictionary<FileInfo, ThumbConfig>() { { destFile, config } };
                }

                if (altSource != null)
                {
                    Logging.LogTrace("File {0} exists - using it as source for smaller thumbs.", altSource.Name);
                    imagePath = altSource;
                }

                // See if there's any conversions to do
                if (destFiles.Any())
                {
                    // First, pre-create the folders for any thumbs we'll be creating
                    destFiles.Select(x => x.Key.DirectoryName)
                            .Distinct().ToList()
                            .ForEach(dir => System.IO.Directory.CreateDirectory(dir));

                    Logging.LogVerbose("Generating thumbnails for {0}", imagePath);

                    var watch = new Stopwatch("ConvertNative", 60000);
                    try
                    {
                        result = await _imageProcessingService.CreateThumbs(imagePath, destFiles);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError("Thumbnail conversion failed for {0}: {1}", imagePath, ex.Message);
                    }
                    finally
                    {
                        watch.Stop();
                        Logging.LogVerbose($"{destFiles.Count()} thumbs created for {imagePath} in {watch.HumanElapsedTime}");
                    }

                    if (result.ThumbsGenerated)
                    {
                        // Generate the perceptual hash from the large thumbnail.
                        var largeThumbPath = GetThumbPath(imagePath, ThumbSize.Large);

                        if (File.Exists(largeThumbPath))
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
                Logging.LogWarning("Skipping thumb generation for missing file...");

        }
        catch (Exception ex)
        {
            Logging.LogTrace("Exception converting thumbnails for {0}: {1}...", imagePath, ex.Message);
        }

        return result;
    }

    public async Task MarkAllImagesForScan()
    {
        using var db = new ImageContext();

        // TODO: Abstract this once EFCore Bulkextensions work in efcore 6
        int updated = await db.Database.ExecuteSqlInterpolatedAsync($"Update imagemetadata Set ThumbLastUpdated = null");

        _statusService.StatusText = $"All {updated} images flagged for thumbnail re-generation.";
    }

    public async Task MarkFolderForScan(Folder folder)
    {
        using var db = new ImageContext();

        int updated = await ImageMetaData.UpdateFields(db, folder, "ThumbLastUpdated", "null");

        if( updated != 0 )
            _statusService.StatusText = $"{updated} images in folder {folder.Name} flagged for thumbnail re-generation.";
    }

    public async Task MarkImagesForScan(ICollection<Image> images)
    {
        using var db = new ImageContext();

        string imageIds = string.Join(",", images.Select(x => x.ImageId));
        string sql = $"Update imagemetadata Set ThumbLastUpdated = null where imageid in ({imageIds})";

        // TODO: Abstract this once EFCore Bulkextensions work in efcore 6
        await db.Database.ExecuteSqlRawAsync(sql);

        var msgText = images.Count == 1 ? $"Image {images.ElementAt(0).FileName}" : $"{images.Count} images";
        _statusService.StatusText = $"{msgText} flagged for thumbnail re-generation.";

        _workService.FlagNewJobs(this);
    }

    public class ThumbProcess : IProcessJob
    {
        public int ImageId { get; set; }
        public ThumbnailService Service { get; set; }
        public bool CanProcess => true;
        public string Name => "Thumbnail Generation";
        public string Description => $"Thumbnail gen for ID:{ImageId}";
        public JobPriorities Priority => JobPriorities.Thumbnails;
        public override string ToString() => Description;

        public async Task Process()
        {
            await Service.CreateThumb(ImageId);
        }
    }

    public JobPriorities Priority => JobPriorities.Thumbnails;

    public async Task<ICollection<IProcessJob>> GetPendingJobs( int maxJobs )
    {
        if (!EnableThumbnailGeneration)
            return new ThumbProcess[0];
        
        using var db = new ImageContext();

        var images = await db.ImageMetaData.Where(x => x.ThumbLastUpdated == null)
                                .OrderByDescending(x => x.LastUpdated)
                                .Take(maxJobs)
                                .Select(x => x.ImageId)
                                .ToListAsync();

        var jobs = images.Select( x => new ThumbProcess { ImageId = x, Service = this})
                        .ToArray();

        return jobs;
    }
}
