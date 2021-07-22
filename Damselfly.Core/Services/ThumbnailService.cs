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
using Damselfly.ML.ObjectDetection;
using Damselfly.Core.Models;
using Damselfly.ML.Face.Accord;
using Damselfly.ML.Face.Azure;
using System.Collections.Concurrent;

namespace Damselfly.Core.Services
{
    
    public class ThumbnailService
    {
        private static string _thumbnailRootFolder;
        private const string _requestRoot = "/images";
        private static int s_maxThreads = GetMaxThreads();
        private readonly ObjectDetector _objectDetector;
        private readonly StatusService _statusService;
        private readonly IndexingService _indexingService;
        private readonly ImageProcessService _imageProcessingService;
        private readonly AccordFaceService _accordFaceService;
        private readonly AzureFaceService _azureFaceService;
        private IDictionary<string, Person> _peopleCache;


        public ThumbnailService( StatusService statusService, ObjectDetector objectDetector,
                        IndexingService indexingService, ImageProcessService imageService,
                        AzureFaceService azureFace, AccordFaceService accordFace )
        {
            _accordFaceService = accordFace;
            _azureFaceService = azureFace;
            _statusService = statusService;
            _objectDetector = objectDetector;
            _indexingService = indexingService;
            _imageProcessingService = imageService;
        }

        private static int GetMaxThreads()
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
            LoadPersonCache();

            if (EnableThumbnailGeneration)
            {
                Logging.Log("Started thumbnail service.");

                var thread = new Thread(new ThreadStart(RunThumbnailScan));
                thread.Name = "ThumbnailThread";
                thread.IsBackground = true;
                thread.Priority = ThreadPriority.Lowest;
                thread.Start();
            }
            else
            {
                Logging.Log("Thumbnail service was disabled.");
            }
        }

        private void RunThumbnailScan()
        {
            while (true)
            {
#if DEBUG
                const int sleepSecs = 5;
#else
                const int sleepSecs = 60;
#endif
                ProcessThumbnailScan().Wait();
                
                Thread.Sleep(1000 * sleepSecs);
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
            var result = await ConvertFile(sourceImage.Image, forceRegeneration);

            sourceImage.ThumbLastUpdated = DateTime.UtcNow;
            sourceImage.Hash = result.ImageHash;

            await DetectObjects(sourceImage.Image);

            return result;
        }

        /// <summary>
        /// Initialise the in-memory cache of people.
        /// </summary>
        /// <param name="force"></param>
        private void LoadPersonCache(bool force = false)
        {
            try
            {
                if (_peopleCache == null || force)
                {
                    var watch = new Stopwatch("LoadTagCache");

                    using (var db = new ImageContext())
                    {
                        // Pre-cache tags from DB.
                        _peopleCache = new ConcurrentDictionary<string, Person>(db.People
                                                                                    .AsNoTracking()
                                                                                    .ToDictionary(k => k.AzurePersonId, v => v));
                        if (_peopleCache.Any())
                            Logging.LogTrace("Pre-loaded cach with {0} people.", _peopleCache.Count());
                    }

                    watch.Stop();
                }
            }
            catch (Exception ex)
            {
                Logging.LogError($"Unexpected exception loading people cache: {ex.Message}");
            }
        }

        public async Task<List<Person>> CreateMissingPeople(IEnumerable<string> personIdsToAdd)
        {
            using ImageContext db = new ImageContext();

            // Find the people that aren't already in the cache and add new ones
            var newPeople = personIdsToAdd.Where(x => !_peopleCache.ContainsKey(x))
                        .Select(x => new Person {
                                AzurePersonId = x,
                                Name = "Unknown",
                                State = Person.PersonState.Unknown
                            }
                        ).ToList();


            if (newPeople.Any())
            {

                Logging.LogTrace("Adding {0} people", newPeople.Count());

                await db.BulkInsert(db.People, newPeople);

                // Add the new items to the cache. 
                newPeople.ForEach(x => _peopleCache[x.AzurePersonId] = x);
            }

            var allTags = personIdsToAdd.Select(x => _peopleCache[x]).ToList();
            return allTags;
        }

        /// <summary>
        /// Detect objects in the image.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private async Task DetectObjects(Image image)
        {
            try
            {
                var foundObjects = new List<ImageObject>();
                var foundFaces = new List<ImageObject>();
                var file = new FileInfo(image.FullPath);
                var thumbSize = ThumbSize.Medium;
                var medThumb = new FileInfo(GetThumbPath(file, thumbSize));

                // Load the bitmap once
                var bitmap = new System.Drawing.Bitmap( medThumb.FullName );

                // First, look for faces
                bool useAzureDetection = false;

                if (_azureFaceService.DetectionType == AzureFaceService.AzureDetection.AllImages)
                {
                    // Skip local face detection and just go straight to Azure
                    useAzureDetection = true;
                }
                else
                {
                    var faces = _accordFaceService.DetectFaces(bitmap);

                    if (faces.Any())
                    {
                        if (_azureFaceService.DetectionType == AzureFaceService.AzureDetection.ImagesWithFaces)
                        {
                            // We've found some faces. Discard them, and reprocess more accurately using Azure
                            useAzureDetection = true;
                        }
                        else
                        {
                            // Azure is disabled, so just use what we've got.

                            if ( faces.Any() )
                                Logging.Log($"Accord.Net found {faces.Count} faces in {medThumb}...");

                            foundFaces.AddRange(faces.Select(x => new ImageObject
                            {
                                ImageId = image.ImageId,
                                RectX = x.FaceRectangle.Left,
                                RectY = x.FaceRectangle.Top,
                                RectHeight = x.FaceRectangle.Height,
                                RectWidth = x.FaceRectangle.Width,
                                Type = ImageObject.ObjectTypes.Face.ToString(),
                                Score = 100
                            }));
                        }
                    }
                }

                if (useAzureDetection)
                {
                    // We got predictions or we're scanning everything - so now let's try the image with Azure.
                    var azureFaces = await _azureFaceService.DetectFaces( bitmap );

                    WriteTransactionCount();

                    if( azureFaces.Any() )
                        Logging.Log($"Azure found {azureFaces.Count} faces in {medThumb}...");

                    // Get a list of the Azure Person IDs
                    var peopleIds = azureFaces.Select(x => x.PersonId.ToString() );

                    // Create any new ones, or pull existing ones back from the cache
                    var people = await CreateMissingPeople( peopleIds );

                    foundFaces.AddRange(azureFaces.Select(x => new ImageObject
                    {
                        ImageId = image.ImageId,
                        RectX = x.Left,
                        RectY = x.Top,
                        RectHeight = x.Height,
                        RectWidth = x.Width,
                        Type = ImageObject.ObjectTypes.Face.ToString(),
                        Score = 100,
                        PersonId = _peopleCache[x.PersonId.ToString()].PersonId
                    }));

                    var peopleToAdd = foundFaces.Select(x => x.Person);
                }

                if ( foundFaces.Any() )
                {
                    // We've found some faces. Add a tagID.
                    const string faceTagName = "Face";
                    var tags = await _indexingService.CreateTagsFromStrings(new List<string> { faceTagName });
                    var faceTagId = tags.Single().TagId;
                    foundFaces.ForEach(x => x.TagId = faceTagId);
                }

                // Next, look for Objects
                var allPredictions = await _objectDetector.DetectObjects( bitmap );

                if (allPredictions.Any())
                {
                    const float predictionThreshold = 0.5f;

                    var validPredictions = allPredictions.Where(x => x.Score >= predictionThreshold).ToList();

                    Logging.LogVerbose($"Discarding {allPredictions.Count - validPredictions.Count} uncertain predictions.");

                    if (validPredictions.Any())
                    {
                        Logging.Log($"Yolo found {validPredictions.Count} objects in {medThumb}...");

                        var allLabels = validPredictions.Select(x => x.Label.Name).Distinct().ToList();

                        var tags = await _indexingService.CreateTagsFromStrings(allLabels);

                        foundObjects.AddRange(validPredictions.Select(x => new ImageObject
                        {
                            ImageId = image.ImageId,
                            TagId = tags.Where(l => l.Keyword == x.Label.Name).Select(x => x.TagId).First(),
                            RectX = (int)x.Rectangle.Left,
                            RectY = (int)x.Rectangle.Top,
                            RectHeight = (int)x.Rectangle.Height,
                            RectWidth = (int)x.Rectangle.Width,
                            Type = ImageObject.ObjectTypes.Object.ToString(),
                            Score = x.Score
                        }));
                    }
                }

                if( foundObjects.Any() || foundFaces.Any() )
                {
                    var allFound = foundObjects.Union(foundFaces).ToList();

                    allFound.ForEach(x =>
                    {
                        ScaleObjectRect(image, ref x, thumbSize);
                        DrawRect(image.FullPath, x);
                    });

                    using var db = new ImageContext();

                    // First, clear out the existing faces and objects - we don't want dupes
                    // TODO: Might need to be smarter about this once we add face names and
                    // Object identification details.
                    await db.BatchDelete(db.ImageObjects.Where(x => x.ImageId.Equals(image.ImageId)));
                    // Now add the objects and faces.
                    await db.BulkInsert(db.ImageObjects, allFound);
                }
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during AI detection: {ex.Message}");
            }
        }

        /// <summary>
        /// Store an aggregated count of Cloud Transactions
        /// </summary>
        private void WriteTransactionCount()
        {
            using var db = new ImageContext();
            var type = CloudTransaction.TransactionType.AzureFace;

            DateTime today = DateTime.UtcNow.Date;

            var count = _azureFaceService.GetAndResetTransCount();

            if (count > 0)
            {
                var todayTrans = db.CloudTransactions.Where(x => x.Date == today && x.TransType == type).FirstOrDefault();

                if (todayTrans == null)
                {
                    todayTrans = new CloudTransaction { Date = today, TransType = type, TransCount = count };
                    db.CloudTransactions.Add(todayTrans);
                }
                else
                {
                    todayTrans.TransCount += count;
                    db.CloudTransactions.Update(todayTrans);
                }

                db.SaveChanges("TransCount");
            }
        }

        /// <summary>
        /// Scales the detected face/object rectangles based on the full-sized image,
        /// since the object detection was done on a smaller thumbnail.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imgObj"></param>
        /// <param name="thumbSize"></param>
        private void ScaleObjectRect(Image image, ref ImageObject imgObj, ThumbSize thumbSize )
        {
            var thumbConfig = thumbConfigs.First(x => x.size == thumbSize);

            float shortestThumbSide = thumbConfig.width < thumbConfig.height ? thumbConfig.width : thumbConfig.height;
            float shortestImgSide = image.MetaData.Width < image.MetaData.Height ? image.MetaData.Width : image.MetaData.Height;
            var ratio = shortestImgSide / shortestThumbSide;

            imgObj.RectX = (int)(imgObj.RectX * ratio);
            imgObj.RectY = (int)(imgObj.RectY * ratio);
            imgObj.RectWidth = (int)(imgObj.RectWidth * ratio);
            imgObj.RectHeight = (int)(imgObj.RectHeight * ratio);
        }

        /// <summary>
        /// Debugging tool.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="imgObj"></param>
        private void DrawRect(string fullPath, ImageObject imgObj)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                string outDir = "/Users/markotway/Desktop/Faces";
                if (!System.IO.Directory.Exists(outDir))
                    System.IO.Directory.CreateDirectory(outDir);

                var output = Path.Combine(outDir, Path.GetFileName(fullPath));

                ImageSharpProcessor.DrawRects(fullPath, imgObj.RectX, imgObj.RectY, imgObj.RectWidth, imgObj.RectHeight, output);
            }
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
