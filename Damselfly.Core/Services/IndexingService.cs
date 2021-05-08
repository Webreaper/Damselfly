using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using MetadataExtractor.Formats.Iptc;
using System.Threading;
using MetadataExtractor.Formats.Jpeg;
using Z.EntityFramework.Plus;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// Core indexing service, which is responsible for scanning the folders on
    /// disk for images, and to ingest them into the DB with all their extracted
    /// metadata, such as size, last modified date, etc., etc.
    /// </summary>
    public class IndexingService
    {
        // Some caching to avoid repeatedly reading tags, cameras and lenses
        // from the DB.
        private IDictionary<string, Models.Tag> _tagCache;
        private IDictionary<string, Camera> _cameraCache;
        private IDictionary<string, Lens> _lensCache;
        private IDictionary<string, FileSystemWatcher> _watchers = new Dictionary<string, FileSystemWatcher>( StringComparer.OrdinalIgnoreCase );

        public static string RootFolder { get; set; }
        public static IndexingService Instance { get; private set; }
        public static bool EnableIndexing { get; set; } = true;
        public static bool EnableThumbnailGeneration { get; set; } = true;

        public IndexingService()
        {
            Instance = this;
        }

        public event Action OnFoldersChanged;

        private void NotifyFolderChanged()
        {
            Logging.LogVerbose($"Folders changed.");

            // TODO - invoke back on dispatcher thread....
            OnFoldersChanged?.Invoke();
        }

        public IEnumerable<Models.Tag> CachedTags {  get { return _tagCache.Values.ToList();  } }

        /// <summary>
        /// Read the metadata, and handle any exceptions.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns>Metadata, or Null if there was an error</returns>
        private IReadOnlyList<MetadataExtractor.Directory> SafeReadImageMetadata( string imagePath )
        {
            IReadOnlyList<MetadataExtractor.Directory> metadata = null;

            if (File.Exists(imagePath))
            {
                try
                {
                    metadata = ImageMetadataReader.ReadMetadata(imagePath);
                }
                catch (ImageProcessingException ex)
                {
                    Logging.Log("Metadata read for image {0}: {1}", imagePath, ex.Message);

                }
                catch (IOException ex)
                {
                    Logging.Log("File error reading metadata for {0}: {1}", imagePath, ex.Message);

                }
            }

            return metadata;
        }


        /// <summary>
        /// Scans an image file on disk for its metadata, using the MetaDataExtractor
        /// library. The image object is populated with the metadata, and the IPTC
        /// keywords are returned back to the caller for processing and ingestion
        /// into the DB.
        /// </summary>
        /// <param name="image">Image object, which will be updated with metadata</param>
        /// <param name="keywords">Array of keyword tags in the image EXIF data</param>
        private void GetImageMetaData(ref ImageMetaData imgMetaData, out string[] keywords)
        {
            var image = imgMetaData.Image;
            keywords = new string[0];

            try
            {
                var watch = new Stopwatch("ReadMetaData");

                IReadOnlyList<MetadataExtractor.Directory> metadata = SafeReadImageMetadata( image.FullPath );

                watch.Stop();

                // Update the timestamp
                imgMetaData.LastUpdated = DateTime.UtcNow;

                if (metadata != null)
                {
                    var jpegDirectory = metadata.OfType<JpegDirectory>().FirstOrDefault();

                    if (jpegDirectory != null)
                    {
                        imgMetaData.Width = jpegDirectory.SafeGetExifInt(JpegDirectory.TagImageWidth);
                        imgMetaData.Width = jpegDirectory.SafeGetExifInt(JpegDirectory.TagImageHeight);
                    }

                    var subIfdDirectory = metadata.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                    if (subIfdDirectory != null)
                    {
                        var desc = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagImageDescription);

                        imgMetaData.Description = FilteredDescription( desc );

                        imgMetaData.DateTaken = subIfdDirectory.SafeGetExifDateTime(ExifDirectoryBase.TagDateTimeOriginal);

                        imgMetaData.Width = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagExifImageHeight);
                        imgMetaData.Height = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagExifImageWidth);

                        if (imgMetaData.Width == 0)
                            imgMetaData.Width = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagImageWidth);
                        if (imgMetaData.Height == 0)
                            imgMetaData.Height = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagImageHeight);

                        imgMetaData.ISO = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagIsoEquivalent);
                        imgMetaData.FNum = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagFNumber);
                        imgMetaData.Exposure = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagExposureTime);

                        var lensMake = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagLensMake);
                        var lensModel = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagLensModel);
                        var lensSerial = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagLensSerialNumber);

                        if (!string.IsNullOrEmpty(lensMake) || !string.IsNullOrEmpty(lensModel))
                            imgMetaData.LensId = GetLens(lensMake, lensModel, lensSerial).LensId;

                        var flash = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagFlash);

                        imgMetaData.FlashFired = ((flash & 0x1) != 0x0);
                    }

                    var IPTCdir = metadata.OfType<IptcDirectory>().FirstOrDefault();

                    if (IPTCdir != null)
                    {
                        var caption = IPTCdir.SafeExifGetString(IptcDirectory.TagCaption);

                        imgMetaData.Caption = FilteredDescription(caption);

                        // Stash the keywords in the dict, they'll be stored later.
                        var keywordList = IPTCdir?.GetStringArray(IptcDirectory.TagKeywords);
                        if (keywordList != null)
                            keywords = keywordList;
                    }

                    var IfdDirectory = metadata.OfType<ExifIfd0Directory>().FirstOrDefault();

                    if (IfdDirectory != null)
                    {
                        var camMake = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagMake);
                        var camModel = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagModel);
                        var camSerial = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagBodySerialNumber);

                        if (!string.IsNullOrEmpty(camMake) || !string.IsNullOrEmpty(camModel))
                        {
                            imgMetaData.CameraId = GetCamera(camMake, camModel, camSerial).CameraId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log("Error reading image metadata for {0}: {1}", image.FullPath, ex.Message);
            }
        }

        private string FilteredDescription(string desc)
        {
            if (! string.IsNullOrEmpty(desc))
            {
                // No point clogging up the DB with thousands
                // of identical default descriptions
                if (desc.Trim().Equals("OLYMPUS DIGITAL CAMERA"))
                    return string.Empty;
            }

            return desc;
        }

        #region Tag, Lens and Camera Caching
        /// <summary>
        /// Get a camera object, for each make/model. Uses an in-memory cache for speed.
        /// </summary>
        /// <param name="make"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private Camera GetCamera( string make, string model, string serial)
        {
            if (_cameraCache == null)
            {
                using var db = new ImageContext();
                _cameraCache = new ConcurrentDictionary<string, Camera>( db.Cameras
                                                                           .AsNoTracking() // We never update, so this is faster
                                                                           .ToDictionary(x => x.Make + x.Model, y => y) );
            }

            string cacheKey = make + model;

            if (string.IsNullOrEmpty(cacheKey))
                return null;

            if (!_cameraCache.TryGetValue(cacheKey, out Camera cam))
            {
                // It's a new one.
                cam = new Camera { Make = make, Model = model, Serial = serial };

                using var db = new ImageContext();
                db.Cameras.Add(cam);
                db.SaveChanges("SaveCamera");

                _cameraCache[cacheKey] = cam;
            }

            return cam;
        }

        /// <summary>
        /// Get a lens object, for each make/model. Uses an in-memory cache for speed.
        /// </summary>
        /// <param name="make"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private Lens GetLens(string make, string model, string serial)
        {
            if (_lensCache == null)
            {
                using ImageContext db = new ImageContext();
                _lensCache = new ConcurrentDictionary<string, Lens>(db.Lenses
                                                                      .AsNoTracking()
                                                                      .ToDictionary(x => x.Make + x.Model, y => y)) ;
            }

            string cacheKey = make + model;

            if (string.IsNullOrEmpty(cacheKey))
                return null;

            if (!_lensCache.TryGetValue(cacheKey, out Lens lens))
            {
                // It's a new one.
                lens = new Lens { Make = make, Model = model, Serial = serial };

                using var db = new ImageContext();
                db.Lenses.Add(lens);
                db.SaveChanges("SaveLens");

                _lensCache[cacheKey] = lens;
            }

            return lens;
        }

        /// <summary>
        /// Initialise the in-memory cache of tags.
        /// </summary>
        /// <param name="force"></param>
        private void LoadTagCache(bool force = false)
        {
            try
            {
                if (_tagCache == null || force)
                {
                    var watch = new Stopwatch("LoadTagCache");

                    using (var db = new ImageContext())
                    {
                        // Pre-cache tags from DB.
                        _tagCache = new ConcurrentDictionary<string, Models.Tag>(db.Tags
                                                                                    .AsNoTracking()
                                                                                    .ToDictionary(k => k.Keyword, v => v));
                        if (_tagCache.Any())
                            Logging.LogTrace("Pre-loaded cach with {0} tags.", _tagCache.Count());
                    }

                    watch.Stop();
                }
            }
            catch (Exception ex)
            {
                Logging.LogError($"Unexpected exception loading tag cache: {ex.Message}");
            }
        }
        #endregion

        /// <summary>
        /// Given a collection of images and their keywords, performs a bulk insert
        /// of them all. This is way more performant than adding the keywords as
        /// each image is indexed, and allows us to bulk-update the freetext search
        /// too.
        /// </summary>
        /// <param name="imageKeywords"></param>
        private void AddTags( IDictionary<Image, string[]> imageKeywords )
        {
            // See if we have any images that were written to the DB and have IDs
            if ( ! imageKeywords.Where( x => x.Key.ImageId != 0 ).Any())
                return;

            var watch = new Stopwatch("AddTags");

            using ImageContext db = new ImageContext();

            try
            {
                var newTags = imageKeywords.Where( x => x.Value != null && x.Value.Any() )
                                        .SelectMany(x => x.Value)
                                        .Distinct()
                                        .Where( x => _tagCache != null && ! _tagCache.ContainsKey( x ))
                                        .Select( x => new Models.Tag { Keyword = x, Type = "IPTC" })
                                        .ToList();


                if (newTags.Any())
                {

                    Logging.LogTrace("Adding {0} tags", newTags.Count());

                    db.BulkInsert(db.Tags, newTags);

                    // Add the new items to the cache. 
                    foreach (var tag in newTags)
                        _tagCache[tag.Keyword] = tag;
                }
            }
            catch (Exception ex)
            {
                Logging.LogError("Exception adding Tags: {0}", ex);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var newImageTags = imageKeywords.SelectMany(i => i.Value.Select(
                                                                    v => new ImageTag
                                                                    {
                                                                        ImageId = i.Key.ImageId,
                                                                        TagId = _tagCache[v].TagId
                                                                    }))
                                                                .ToList();

                    // Note that we need to delete all of the existing tags for an image,
                    // and then insert all of the new tags. This is so that if somebody adds
                    // one tag, and removes another, we maintain the list correctly.
                    Logging.LogTrace($"Updating {newImageTags.Count()} ImageTags");

                    // TODO: This should be in the abstract model
                    if (!ImageContext.ReadOnly)
                    {

                        // TODO: Push these down to the abstract model
                        db.ImageTags.Where(y => newImageTags.Select(x => x.ImageId)
                                .Contains(y.ImageId))
                                .Delete();

#if ! DEBUG
                        db.BulkInsertOrUpdate(db.ImageTags, newImageTags, (x) => { return x.TagId == 0; } );;
#endif

                        transaction.Commit();

                        db.FullTextTags(false);
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogError("Exception adding ImageTags: {0}", ex);
                }
            }

            watch.Stop();
        }

        /// <summary>
        /// Indexes all of the images in a folder, optionally filtering for a last-mod
        /// threshold and only indexing those images which have changed since that date.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="threshold"></param>
        /// <param name="parent"></param>
        public void IndexFolder(DirectoryInfo folder, Folder parent, bool isFullIndex )
        {
            Folder folderToScan = null;

            // Get all the sub-folders on the disk, but filter out
            // ones we're not interested in.
            var subFolders = folder.SafeGetSubDirectories()
                                    .Where( x => x.IsMonitoredFolder() )
                                    .ToList();

            try
            {
                using (var db = new ImageContext())
                {
                    // Load the existing folder and its images from the DB
                    folderToScan = db.Folders
                                .Where(x => x.Path.Equals(folder.FullName))
                                .Include(x => x.Images)
                                .FirstOrDefault();

                    if (folderToScan == null)
                    {
                        Logging.LogVerbose("Scanning new folder: {0}\\{1}", folder.Parent.Name, folder.Name);
                        folderToScan = new Folder { Path = folder.FullName };
                    }
                    else
                        Logging.LogVerbose("Scanning existing folder: {0}\\{1} ({2} images in DB)", folder.Parent.Name, folder.Name, folderToScan.Images.Count());

                    if (parent != null)
                        folderToScan.ParentFolderId = parent.FolderId;

                    bool foldersChanged = false;

                    if (folderToScan.FolderId == 0)
                    {
                        Logging.Log($"Adding new folder: {folderToScan.Path}");
                        // New folder, add it. 
                        db.Folders.Add(folderToScan);
                        db.SaveChanges("AddFolders");
                        foldersChanged = true;
                    }

                    // Now, check for missing folders, and clean up if appropriate.
                    foldersChanged = RemoveMissingChildDirs(db, folderToScan) || foldersChanged;

                    if (foldersChanged)
                        NotifyFolderChanged();
                }

                // Now scan the images:
                ScanFolderImages( folderToScan, isFullIndex);

                CreateFileWatcher(folder);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Unexpected exception scanning folder {folderToScan.Name}: {ex.Message}");
                if( ex.InnerException != null )
                    Logging.LogError($" Inner exception: {ex.InnerException.Message}");
            }

            // Scan subdirs recursively.
            foreach (var sub in subFolders)
            {
                IndexFolder(sub, folderToScan, isFullIndex);
            }
        }

        /// <summary>
        /// Checks the folder, and any recursive children, to ensure it still exists
        /// on the disk. If it doesn't, removes the child folders from the databas.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="folderToScan"></param>
        /// <returns>True if any folders were updated/changed</returns>
        private bool RemoveMissingChildDirs(ImageContext db, Folder folderToScan)
        {
            bool foldersChanged = false;

            try
            {
                // Now query the DB for child folders of our current folder
                var dbChildDirs = db.Folders.Where(x => x.ParentFolderId == folderToScan.FolderId).ToList();

                foreach (var childFolder in dbChildDirs)
                {
                    // Depth-first removal of child folders
                    foldersChanged = RemoveMissingChildDirs(db, childFolder);
                }

                // ...and then look for any DB folders that aren't included in the list of sub-folders.
                // That means they've been removed from the disk, and should be removed from the DB.
                var missingDirs = dbChildDirs.Where(f => !System.IO.Directory.Exists(f.Path)).ToList();

                if (missingDirs.Any())
                {
                    missingDirs.ForEach(x =>
                    {
                        Logging.LogVerbose("Deleting folder {0}", x.Path);
                        RemoveFileWatcher(x.Path);
                    });

                    db.RemoveRange(missingDirs);

                    Logging.Log("Removing {0} deleted folders...", missingDirs.Count());
                    // Don't use bulk delete; we want EFCore to remove the linked images
                    db.SaveChanges("DeleteFolders");
                    foldersChanged = true;
                }

            }
            catch( Exception ex )
            {
                Logging.LogError($"Unexpected exception scanning for removed folders {folderToScan.Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Logging.LogError($" Inner exception: {ex.InnerException.Message}");
            }
            return foldersChanged;
        }

        /// <summary>
        /// For a given folder, scans the disk to find all the images in that folder,
        /// and then indexes all of those images for metadata etc. Optionally takes
        /// a last-mod threshold which, if set, will mean that only images changed
        /// since that date will be processed.
        /// </summary>
        /// <param name="folderToScan"></param>
        /// <param name="force">Force the folder to be scanned</param>
        /// <returns></returns>
        private bool ScanFolderImages(Folder folderToScan, bool force = false)
        {
            int folderImageCount = 0;

            var folder = new DirectoryInfo(folderToScan.Path);
            var allImageFiles = folder.SafeGetImageFiles();

            if (allImageFiles == null)
            {
                // Null here means we weren't able to read the contents of the directory.
                // So bail, and give up on this folder altogether.
                return false;
            }

            // First, see if images have been added or removed since we last indexed.
            // If so, we disregard the last scan date of the folder and force the
            // update. 
            int knownDBImages = folderToScan.Images.Count();

            if( knownDBImages != allImageFiles.Count() )
            {
                Logging.LogVerbose($"New or removed images in folder {folderToScan.Name}.");
                force = true;
            }

            if (folderToScan.FolderScanDate != null && !force)
            {
                return true;
            }

            using (var db = new ImageContext())
            {
                var watch = new Stopwatch("ScanFolderFiles");

                // Select just JPGs
                var imageFiles = allImageFiles.Where(x => x.IsImageFileType() ).ToList();
                folderImageCount = imageFiles.Count();

                int newImages = 0, updatedImages = 0;
                foreach (var file in imageFiles)
                {
                    try
                    {
                        var dbImage = folderToScan.Images.FirstOrDefault(x => x.FileName.Equals(file.Name, StringComparison.OrdinalIgnoreCase));

                        if (dbImage != null)
                        {
                            // See if the image has changed since we last indexed it
                            bool fileChanged = file.FileIsMoreRecentThan(dbImage.LastUpdated);

                            if( !fileChanged )
                            {
                                // File hasn't changed. Look for a sidecar to see if it's been modified.
                                var sidecar = dbImage.GetSideCar();

                                if (sidecar != null )
                                {
                                    // If there's a sidecar, see if that's changed.
                                    fileChanged = sidecar.Filename.FileIsMoreRecentThan(dbImage.LastUpdated);
                                }
                            }

                            if( !fileChanged)
                            {
                                Logging.LogTrace($"Indexed image {dbImage.FileName} unchanged - skipping.");
                                continue;
                            }
                        }

                        Image image = dbImage;

                        if (image == null)
                        {
                            image = new Image { FileName = file.Name };
                        }

                        // Store some info about the disk file
                        image.FileSizeBytes = (ulong)file.Length;
                        image.FileCreationDate = file.CreationTimeUtc;
                        image.FileLastModDate = file.LastWriteTimeUtc;

                        image.Folder = folderToScan;
                        image.FlagForMetadataUpdate();

                        if (dbImage == null)
                        {
                            // Default the sort date to the last write time. It'll get updated
                            // later during indexing to set it to the date-taken date.
                            image.SortDate = file.LastWriteTimeUtc;

                            Logging.LogTrace("Adding new image {0}", image.FileName);
                            folderToScan.Images.Add(image);
                            newImages++;
                        }
                        else
                        {
                            db.Images.Update(image);
                            updatedImages++;
                        }
                    }
                    catch( Exception ex )
                    {
                        Logging.LogError($"Exception while scanning for new image {file}: {ex.Message}");
                    }
                }

                // Now look for files to remove.
                // TODO - Sanity check that these don't hit the DB
                var filesToRemove = folderToScan.Images.Select(x => x.FileName).Except(imageFiles.Select(x => x.Name));
                var dbImages = folderToScan.Images.Select(x => x.FileName);
                var imagesToDelete = folderToScan.Images
                                    .Where(x => filesToRemove.Contains(x.FileName))
                                    .ToList();

                if (imagesToDelete.Any())
                {
                    imagesToDelete.ForEach(x => Logging.LogVerbose("Deleting image {0} (ID: {1})", x.FileName, x.ImageId));

                    // Removing these will remove the associated ImageTag and selection references.
                    db.Images.RemoveRange(imagesToDelete);
                }

                // Now update the folder to say we've processed it
                folderToScan.FolderScanDate = DateTime.UtcNow;
                db.Folders.Update(folderToScan);

                db.SaveChanges("FolderImageScan");

                watch.Stop();

                StatusService.Instance.StatusText = string.Format("Indexed folder {0}: processed {1} images ({2} new, {3} updated, {4} removed) in {5}.",
                        folderToScan.Name,folderToScan.Images.Count(), newImages, updatedImages, imagesToDelete.Count(), watch.HumanElapsedTime);
            }

            return true;
        }

        /// <summary>
        /// Index an individual folder
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public bool IndexFolder(Folder folder)
        {
            return ScanFolderImages(folder, true);
        }

        public void RunMetaDataScans()
        {
            Logging.LogVerbose("Metadata scan thread starting...");

            try
            {
                using var db = new ImageContext();

                const int batchSize = 100;
                bool complete = false;

                while( true )
                {
                    var queueQueryWatch = new Stopwatch("MetaDataQueueQuery", -1);

                    // Find all images where there's either no metadata, or where the image or sidecar file 
                    // was updated more recently than the image metadata
                    var imagesToScan = db.Images.Where( x => x.MetaData == null ||
                                                        x.LastUpdated > x.MetaData.LastUpdated )
                                            .OrderByDescending( x => x.LastUpdated )
                                            .Take(batchSize)
                                            .Include(x => x.Folder)
                                            .Include(x => x.MetaData)
                                            .ToArray();

                    queueQueryWatch.Stop();

                    complete = !imagesToScan.Any();

                    if (!complete)
                    {
                        var batchWatch = new Stopwatch("MetaDataBatch", 100000);

                        // Aggregate stuff that we'll collect up as we scan
                        var imageKeywords = new ConcurrentDictionary<Image, string[]>();

                        var newMetadataEntries = new List<ImageMetaData>();
                        var updatedEntries = new List<ImageMetaData>();
                        var updatedImages = new List<Image>();

                        foreach (var img in imagesToScan)
                        {
                            try
                            {
                                ImageMetaData imgMetaData = img.MetaData;

                                if (imgMetaData == null)
                                {
                                    // New metadata
                                    imgMetaData = new ImageMetaData { ImageId = img.ImageId, Image = img };
                                    newMetadataEntries.Add(imgMetaData);
                                }
                                else
                                    updatedEntries.Add(imgMetaData);

                                // Scan the image from the 
                                GetImageMetaData(ref imgMetaData, out var exifKeywords);

                                // Scan for sidecar files
                                var sideCarTags = GetSideCarKeywords(img, exifKeywords);

                                if (sideCarTags.Any())
                                {
                                    // See if we've enabled the option to write any sidecar keywords to IPTC keywords
                                    // if they're missing in the EXIF data of the image.
                                    if (ConfigService.Instance.GetBool(ConfigSettings.ImportSidecarKeywords))
                                    {
                                        // Now, submit the tags; note they won't get created immediately, but in batch.
                                        Logging.Log($"Applying {sideCarTags.Count} keywords from sidecar files to image {img.FileName}");
                                        // Fire and forget this asynchronously - we don't care about waiting for it
                                        _ = MetaDataService.Instance.UpdateTagsAsync(new[] { img }, sideCarTags, null);
                                    }
                                }

                                // Now combine - with case insensitivity.
                                var allKeywords = sideCarTags.Union(exifKeywords, StringComparer.OrdinalIgnoreCase);

                                // Now we have a list of all of the keywords found in the image
                                // and the sidecar.
                                if (allKeywords.Any())
                                    imageKeywords[img] = allKeywords.ToArray();

                                if (img.SortDate != imgMetaData.DateTaken)
                                {
                                    // Update the image sort date with the date taken
                                    img.SortDate = imgMetaData.DateTaken;
                                    img.FlagForMetadataUpdate();
                                    updatedImages.Add(img);
                                }
                            }
                            catch ( Exception ex )
                            {
                                Logging.LogError($"Exception caught during metadata scan for {img.FullPath}: {ex.Message}.");
                            }
                        }

                        var saveWatch = new Stopwatch("MetaDataSave");
                        Logging.LogTrace($"Adding {newMetadataEntries.Count()} and updating {updatedEntries.Count()} metadata entries.");

                        db.BulkInsert(db.ImageMetaData, newMetadataEntries);
                        db.BulkUpdate(db.ImageMetaData, updatedEntries);
                        db.BulkUpdate(db.Images, updatedImages);

                        saveWatch.Stop();

                        var tagWatch = new Stopwatch("AddTagsSave");

                        // Now save the tags
                        AddTags( imageKeywords );

                        tagWatch.Stop();

                        batchWatch.Stop();

                        Logging.Log($"Completed metadata scan batch: {imagesToScan.Length} images, {newMetadataEntries.Count} added, {updatedEntries.Count} updated, {imageKeywords.Count} keywords added.");
                        Logging.LogVerbose($"Time for metadata scan batch: {batchWatch.HumanElapsedTime}, save: {saveWatch.HumanElapsedTime}, tag writes: {tagWatch.HumanElapsedTime}.");
                    }

                    Thread.Sleep(28 * 1000);
                }
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception caught during metadata scan: {ex}");
            }
        }

        /// <summary>
        /// Some image editing apps such as Lightroom, On1, etc., do not persist the keyword metadata
        /// in the images by default. This can mean you keyword-tag them, but those keywords are only
        /// stored in the sidecars. Damselfly only scans keyword metadata from the EXIF image data
        /// itself.
        /// So to rectify this, we can either read the sidecar files for those keywords, and optionally
        /// write the missing keywords to the Exif Metadata as we index them.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="keywords"></param>
        private List<string> GetSideCarKeywords( Image img, string[] keywords )
        {
            var sideCarTags = new List<string>();

            var sidecar = img.GetSideCar();

            if( sidecar != null )
            {
                var imageKeywords = keywords.Select(x => x.RemoveSmartQuotes());
                var sidecarKeywords = sidecar.GetKeywords().Select( x => x.RemoveSmartQuotes() );

                var missingKeywords = sidecarKeywords
                                         .Except(imageKeywords, StringComparer.OrdinalIgnoreCase)
                                         .ToList();

                if (missingKeywords.Any())
                {
                    Logging.Log($"Image {img.FileName} is missing {missingKeywords.Count} keywords present in the {sidecar.Type} Sidecar.");
                    sideCarTags = sideCarTags.Union(missingKeywords, StringComparer.OrdinalIgnoreCase).ToList();
                }
            }

            return sideCarTags;
        }

        public void StartService()
        {
            Logging.Log("Starting indexing service.");

            var indexthread = new Thread( new ThreadStart(() => { RunIndexing(); } ));
            indexthread.Name = "IndexingThread";
            indexthread.IsBackground = true;
            indexthread.Priority = ThreadPriority.Lowest;
            indexthread.Start();

            var metathread = new Thread(new ThreadStart(() => { RunMetaDataScans(); }));
            metathread.Name = "MetaDataThread";
            metathread.IsBackground = true;
            metathread.Priority = ThreadPriority.Lowest;
            metathread.Start();
        }

        public void PerformFullIndex()
        {
            // Perform a full index at startup
            StatusService.Instance.StatusText = "Full Indexing starting...";
            var root = new DirectoryInfo(RootFolder);

            var watch = new Stopwatch("CompleteIndex", -1);

            IndexFolder(root, null, true);

            watch.Stop();

            StatusService.Instance.StatusText = "Full Indexing Complete.";
        }

        private void RunIndexing()
        {
            LoadTagCache();

            PerformFullIndex();

            while ( true )
            {
                Logging.LogVerbose("Polling for pending folder index changes.");

                using var db = new ImageContext();

                const int batchSize = 50;

                // First, take all the queued folder changes and persist them to the DB
                // by setting the FolderScanDate to null.
                var folders = new List<string>();

                while (folderQueue.TryDequeue(out var folder))
                {
                    Logging.Log($"Flagging change for folder: {folder}");
                    folders.Add(folder);
                }

                if( folders.Any() )
                {
#if false // TODO: See https://github.com/Webreaper/Damselfly/issues/96
                    // Now, update any folders to set their scan date to null
                    var pendingFolders = db.Folders
                                           .Where(f => folders.Contains(f.Path))
                                           .BatchUpdate( f => new Folder { FolderScanDate = null } );
#else
                    var pendingFolders = db.Folders.Where(f => folders.Contains(f.Path));
                    foreach (var f in pendingFolders)
                    {
                        f.FolderScanDate = null;
                        db.Update(f);
                    }

                    db.SaveChanges("PendingFolders");
#endif
                }

                // Now, see if there's any folders that have a null scan date.
                var foldersToIndex = db.Folders.Where(x => x.FolderScanDate == null)
                                               .Take( batchSize )
                                               .ToList();

                if( foldersToIndex.Any() )
                {
                    StatusService.Instance.StatusText = $"Detected {foldersToIndex.Count()} folders with new/changed images.";

                    foreach ( var folder in foldersToIndex )
                    {
                        var dir = new DirectoryInfo(folder.Path);
                        // Scan the folder for subdirs
                        IndexFolder(dir, null, false);
                    }
                }

                Thread.Sleep(30 * 1000);
            }
        }

        private void RemoveFileWatcher( string path )
        {
            if( _watchers.TryGetValue( path, out var fsw ) )
            {
                Logging.Log($"Removing FileWatcher for {path}");

                _watchers.Remove(path);

                fsw.EnableRaisingEvents = false;
                fsw.Changed -= OnChanged;
                fsw.Created -= OnChanged;
                fsw.Deleted -= OnChanged;
                fsw.Renamed -= OnRenamed;
                fsw.Error -= WatcherError;
                fsw = null;
            }
        }

        private static ConcurrentQueue<string> folderQueue = new ConcurrentQueue<string>();

        private void CreateFileWatcher(DirectoryInfo path)
        {
            if (!_watchers.ContainsKey(path.FullName) )
            {
                try
                {
                    var watcher = new FileSystemWatcher();

                    Logging.LogVerbose($"Creating FileWatcher for {path}");

                    watcher.Path = path.FullName;

                    // Watch for changes in LastAccess and LastWrite
                    // times, and the renaming of files.
                    watcher.NotifyFilter = NotifyFilters.LastWrite
                                          | NotifyFilters.FileName
                                          | NotifyFilters.Size
                                          | NotifyFilters.DirectoryName;

                    // Add event handlers.
                    watcher.Changed += OnChanged;
                    watcher.Created += OnChanged;
                    watcher.Deleted += OnChanged;
                    watcher.Renamed += OnRenamed;
                    watcher.Error += WatcherError;

                    // Store it in the map
                    _watchers[path.FullName] = watcher;

                    // Begin watching.
                    watcher.EnableRaisingEvents = true;
                }
                catch( Exception ex )
                {
                    Logging.LogError($"Exception creating filewatcher for {path}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Process disk-level inotify changes. Note that this should be *very*
        /// fast to keep up with updates as they come in. So we put all distinct
        /// changes into a queue and then return, and the queue contents will be
        /// processed in batch later. This has the effect of us being able to
        /// collect up a conflated list of actual changes with minimal blocking.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="changeType"></param>
        private static void FlagFolderForRescan( FileInfo file, WatcherChangeTypes changeType )
        {
            using var db = new ImageContext();

            var folder = file.Directory.FullName;

            // If it's hidden, or already in the queue, ignore it.
            if (file.IsHidden() || folderQueue.Contains(folder))
                return;

            // Ignore non images, and hidden files/folders.
            if (file.IsDirectory() || file.IsImageFileType() || file.IsSidecarFileType() )
            {
                Logging.Log($"FileWatcher: adding to queue: {folder} {changeType}");
                folderQueue.Enqueue(folder);
            }
        }

        private static void WatcherError(object sender, ErrorEventArgs e)
        {
            // TODO - need to catch many of these and abort - if the inotify count is too large
            Logging.LogError($"Flagging Error for folder: {e.GetException().Message}");
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            Logging.LogVerbose($"FileWatcher: {e.FullPath} {e.ChangeType}");

            var file = new FileInfo(e.FullPath);

            FlagFolderForRescan(file, e.ChangeType);
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            Logging.LogVerbose($"FileWatcher: {e.OldFullPath} => {e.FullPath} {e.ChangeType}");

            var oldfile = new FileInfo(e.OldFullPath);
            var newfile = new FileInfo(e.FullPath);

            FlagFolderForRescan(oldfile, e.ChangeType);
            FlagFolderForRescan(newfile, e.ChangeType);
        }
    }
}
