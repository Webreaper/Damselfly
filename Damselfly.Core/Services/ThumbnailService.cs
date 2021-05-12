using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Damselfly.Core.Utils;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.EntityFrameworkCore;
using MetadataExtractor.Formats.Jpeg;
using Damselfly.Core.ImageProcessing;
using System.Threading.Tasks;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.Services
{
    
    public class ThumbnailService
    {
        private static string _thumbnailRootFolder;
        private const string _requestRoot = "/images";
        private static readonly int s_maxThreads = Math.Max( Environment.ProcessorCount / 2, 2 );

        public static ThumbnailService Instance { get; private set; }
        public static string PicturesRoot { get; set; }
        public static bool UseGraphicsMagick { get; set; }
        public static bool Synology { get; set; }
        public static string RequestRoot { get { return _requestRoot; } }

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
                string extension = Path.GetExtension(imageFile.Name);
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
            new ThumbConfig{ width = 1280, height = 1280, size = ThumbSize.ExtraLarge, useAsSource = true, batchGenerate = false},
            new ThumbConfig{ width = 800, height = 800, size = ThumbSize.Large, useAsSource = true },
            new ThumbConfig{ width = 640, height = 640, size = ThumbSize.Big, batchGenerate = false},
            new ThumbConfig{ width = 320, height = 320, size = ThumbSize.Medium },
            new ThumbConfig{ width = 160, height = 120, size = ThumbSize.Preview, cropToRatio = true, batchGenerate = false },
            new ThumbConfig{ width = 120, height = 120, size = ThumbSize.Small, cropToRatio = true }
        };

        public ThumbnailService()
        {
            Instance = this;

            if (Synology)
            {
                Logging.Log("Synology OS detected - using native thumbnail structure.");
            }
        }

        private void GetImageSize(string fullPath, out int width, out int height)
        {
            IReadOnlyList<MetadataExtractor.Directory> metadata;

            width = height = 0;
            metadata = ImageMetadataReader.ReadMetadata(fullPath);

            var jpegDirectory = metadata.OfType<JpegDirectory>().FirstOrDefault();

            if (jpegDirectory != null)
            {
                width = jpegDirectory.SafeGetExifInt(JpegDirectory.TagImageWidth);
                height = jpegDirectory.SafeGetExifInt(JpegDirectory.TagImageHeight);
                if (width == 0 || height == 0)
                {
                    var subIfdDirectory = metadata.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                    width = jpegDirectory.SafeGetExifInt(ExifDirectoryBase.TagExifImageWidth);
                    height = jpegDirectory.SafeGetExifInt(ExifDirectoryBase.TagExifImageHeight);
                }
            }
        }

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

                if( ! destFile.Directory.Exists )
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
                            GetImageSize(destFile.FullName, out actualWidth, out actualHeight);

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

        public void StartService()
        {
            Logging.Log("Started thumbnail service.");

            var thread = new Thread(new ThreadStart( RunThumbnailScan ));
            thread.Name = "ThumbnailThread";
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;
            thread.Start();
        }

        private void RunThumbnailScan()
        {
            while (true)
            {
                ProcessThumbnailScan().Wait();
                
                Thread.Sleep(1000 * 60);
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
                                        .ThenInclude( x => x.Folder )
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
                        Logging.LogError($"Exception during parallelised thumbnail generation: {ex.Message}");
                    }

                    // Write the timestamps for the newly-generated thumbs.
                    Logging.LogVerbose("Writing thumbnail generation timestamp updates to DB.");

                    var updateWatch = new Stopwatch("BulkUpdateThumGenDate");
                    await db.BulkUpdate( db.ImageMetaData, imagesToScan.ToList() );
                    updateWatch.Stop();

                    watch.Stop();

                    StatusService.Instance.StatusText = $"Completed thumbnail generation batch ({imagesToScan.Length} images in {watch.HumanElapsedTime}).";

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
        public async Task<ImageProcessResult> CreateThumbs(Models.ImageMetaData sourceImage, bool forceRegeneration )
        {
            var result = await ConvertFile(sourceImage.Image, forceRegeneration);

            sourceImage.ThumbLastUpdated = DateTime.UtcNow;
            sourceImage.Hash = result.ImageHash;

            return result;
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
                        Logging.LogVerbose("Generating thumbnails for {0}", imagePath);

                        var watch = new Stopwatch("ConvertNative", 60000);
                        try
                        {
                            result = await ImageProcessService.Instance.CreateThumbs(imagePath, destFiles);
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
    }
}
