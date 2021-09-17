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
using System.Threading.Tasks;
using TagTypes = Damselfly.Core.Models.Tag.TagTypes;
using Damselfly.Core.Utils.Constants;

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
        public static bool EnableIndexing { get; set; } = true;
        private readonly StatusService _statusService;
        private readonly MetaDataService _metadataService;
        private readonly ConfigService _configService;
        private readonly ImageProcessService _imageProcessService;

        public IndexingService( StatusService statusService, MetaDataService metaData, 
            ImageProcessService imageService, ConfigService config )
        {
            _statusService = statusService;
            _configService = config;
            _metadataService = metaData;
            _imageProcessService = imageService;
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

                        imgMetaData.DateTaken = subIfdDirectory.SafeGetExifDateTime(ExifDirectoryBase.TagDateTimeDigitized);

                        if( imgMetaData.DateTaken == DateTime.MinValue )
                           imgMetaData.DateTaken = subIfdDirectory.SafeGetExifDateTime(ExifDirectoryBase.TagDateTimeOriginal);

                        imgMetaData.Height = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagExifImageHeight);
                        imgMetaData.Width = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagExifImageWidth);

                        if (imgMetaData.Width == 0)
                            imgMetaData.Width = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagImageWidth);
                        if (imgMetaData.Height == 0)
                            imgMetaData.Height = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagImageHeight);

                        imgMetaData.ISO = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagIsoEquivalent);
                        imgMetaData.FNum = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagFNumber);
                        imgMetaData.Exposure = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagExposureTime);

                        var lensMake = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagLensMake);
                        var lensModel = subIfdDirectory.SafeExifGetString("Lens Model");
                        var lensSerial = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagLensSerialNumber);

                        // If there was no lens make/model, it may be because it's in the Makernotes. So attempt
                        // to extract it. This code definitely works for a Leica Panasonic lens on a Panasonic body.
                        // It may not work for other things.
                        if (string.IsNullOrEmpty(lensMake) || string.IsNullOrEmpty(lensModel))
                        {
                            var makerNoteDir = metadata.FirstOrDefault(x => x.Name.Contains("Makernote", StringComparison.OrdinalIgnoreCase));
                            if( makerNoteDir != null )
                            {
                                if (string.IsNullOrEmpty(lensModel) )
                                    lensModel = makerNoteDir.SafeExifGetString("Lens Type");
                            }
                        }

                        if (!string.IsNullOrEmpty(lensMake) || !string.IsNullOrEmpty(lensModel))
                        {
                            if (string.IsNullOrEmpty(lensModel) || lensModel == "N/A")
                                lensModel = "Generic " + lensMake;

                            imgMetaData.LensId = GetLens(lensMake, lensModel, lensSerial).LensId;
                        }

                        var flash = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagFlash);

                        imgMetaData.FlashFired = ((flash & 0x1) != 0x0);
                    }

                    var IPTCdir = metadata.OfType<IptcDirectory>().FirstOrDefault();

                    if (IPTCdir != null)
                    {
                        var caption = IPTCdir.SafeExifGetString(IptcDirectory.TagCaption);
                        var byline = IPTCdir.SafeExifGetString(IptcDirectory.TagByLine);
                        var source = IPTCdir.SafeExifGetString(IptcDirectory.TagSource);

                        imgMetaData.Caption = FilteredDescription(caption);
                        imgMetaData.Copyright = IPTCdir.SafeExifGetString(IptcDirectory.TagCopyrightNotice);
                        imgMetaData.Credit = IPTCdir.SafeExifGetString(IptcDirectory.TagCredit);

                        if (string.IsNullOrEmpty(imgMetaData.Credit) && !string.IsNullOrEmpty(source))
                            imgMetaData.Credit = source;

                        if (!string.IsNullOrEmpty(byline))
                        {
                            if( ! string.IsNullOrEmpty( imgMetaData.Credit ))
                                imgMetaData.Credit += $" ({byline})";
                            else
                                imgMetaData.Credit += $"{byline}";
                        }

                        // Stash the keywords in the dict, they'll be stored later.
                        var keywordList = IPTCdir?.GetStringArray(IptcDirectory.TagKeywords);
                        if (keywordList != null)
                            keywords = keywordList;
                    }

                    var IfdDirectory = metadata.OfType<ExifIfd0Directory>().FirstOrDefault();

                    if (IfdDirectory != null)
                    {
                        var orientation = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagOrientation);
                        var camMake = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagMake);
                        var camModel = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagModel);
                        var camSerial = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagBodySerialNumber);

                        if (!string.IsNullOrEmpty(camMake) || !string.IsNullOrEmpty(camModel))
                        {
                            imgMetaData.CameraId = GetCamera(camMake, camModel, camSerial).CameraId;
                        }

                        if( NeedToSwitchWidthAndHeight( orientation ) )
                        {
                            // It's orientated rotated. So switch the height and width
                            var temp = imgMetaData.Width;
                            imgMetaData.Width = imgMetaData.Height;
                            imgMetaData.Height = temp;
                        }
                    }
                }

                if (imgMetaData.DateTaken == DateTime.MinValue)
                    DumpMetaData(image, metadata);
            }
            catch (Exception ex)
            {
                Logging.Log("Error reading image metadata for {0}: {1}", image.FullPath, ex.Message);
            }
        }

        /// <summary>
        /// These are the orientation strings:
        ///    "Top, left side (Horizontal / normal)",
        ///    "Top, right side (Mirror horizontal)",
        ///    "Bottom, right side (Rotate 180)", "Bottom, left side (Mirror vertical)",
        ///    "Left side, top (Mirror horizontal and rotate 270 CW)",
        ///    "Right side, top (Rotate 90 CW)",
        ///    "Right side, bottom (Mirror horizontal and rotate 90 CW)",
        ///    "Left side, bottom (Rotate 270 CW)"
        /// </summary>
        /// <param name="orientation"></param>
        /// <returns></returns>
        private bool NeedToSwitchWidthAndHeight(string orientation) =>
            orientation switch
            {
                "5" => true,
                "6" => true,
                "7" => true,
                "8" => true,
                "Top, left side (Horizontal / normal)" => false,
                "Top, right side (Mirror horizontal)" => false,
                "Bottom, right side (Rotate 180)" => false,
                "Bottom, left side (Mirror vertical)" => false,
                "Left side, top (Mirror horizontal and rotate 270 CW)" => true,
                "Right side, top (Rotate 90 CW)" => true,
                "Right side, bottom (Mirror horizontal and rotate 90 CW)" => true,
                "Left side, bottom (Rotate 270 CW)" => true,
                _ => false
            };

        /// <summary>
        /// Dump metadata out in tracemode.
        /// </summary>
        /// <param name="metadata"></param>
        private void DumpMetaData(Image img, IReadOnlyList<MetadataExtractor.Directory> metadata)
        {
            Logging.LogVerbose($"Metadata dump for: {img.FileName}:");
            foreach (var dir in metadata)
            {
                Logging.LogVerbose($" Directory: {dir.Name}:");
                foreach (var tag in dir.Tags)
                {
                    Logging.LogVerbose($"  Tag: {tag.Name} = {tag.Description}");
                }
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

        /// <summary>
        /// Return a tag by its ID.
        /// TODO: Is this faster, or slower than a DB query, given it means iterating
        /// a collection of, say, 10,000 tags. Probably faster, but perhaps we should
        /// maintain a dict of ID => tag?
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public Models.Tag GetTag( int tagId )
        {
            var tag = _tagCache.Values.Where(x => x.TagId == tagId).FirstOrDefault();

            return tag;
        }

        public Models.Tag GetTag(string keyword)
        {
            // TODO: Should we make the tag-cache key case-insensitive? What would happen?!
            var tag = _tagCache.Values.Where(x => x.Keyword.Equals( keyword, StringComparison.OrdinalIgnoreCase) ).FirstOrDefault();

            return tag;
        }
        #endregion

        public async Task<List<Models.Tag>> CreateTagsFromStrings(IEnumerable<string> tags)
        {
            using ImageContext db = new ImageContext();

            // Find the tags that aren't already in the cache
            var newTags = tags.Where( x => ! _tagCache.ContainsKey(x) )
                        .Select(x => new Models.Tag { Keyword = x, TagType = TagTypes.IPTC })
                        .ToList();


            if (newTags.Any())
            {

                Logging.LogTrace("Adding {0} tags", newTags.Count());

                await db.BulkInsert(db.Tags, newTags);

                // Add the new items to the cache. 
                newTags.ForEach(x => _tagCache[x.Keyword] = x);
            }

            var allTags = tags.Select(x => _tagCache[x]).ToList();
            return allTags;
        }

        /// <summary>
        /// Given a collection of images and their keywords, performs a bulk insert
        /// of them all. This is way more performant than adding the keywords as
        /// each image is indexed, and allows us to bulk-update the freetext search
        /// too.
        /// </summary>
        /// <param name="imageKeywords"></param>
        /// <param name="type"></param>
        private async Task AddTags( IDictionary<Image, string[]> imageKeywords )
        {
            // See if we have any images that were written to the DB and have IDs
            if ( ! imageKeywords.Where( x => x.Key.ImageId != 0 ).Any())
                return;

            var watch = new Stopwatch("AddTags");

            using ImageContext db = new ImageContext();

            try
            {
                // First, find all the distinct keywords, and check whether
                // they're in the cache. If not, create them in the DB.
                var allKeywords = imageKeywords.Where(x => x.Value != null && x.Value.Any() )
                                               .SelectMany( x => x.Value ).Distinct();

                await CreateTagsFromStrings(allKeywords);
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
                        await db.BatchDelete(db.ImageTags.Where(y => newImageTags.Select(x => x.ImageId)
                               .Contains(y.ImageId)));

                        await db.BulkInsert( db.ImageTags, newImageTags );;

                        transaction.Commit();

                        await db.GenFullText(false);
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
        /// <param name="parent"></param>
        public void IndexFolder(DirectoryInfo folder, Folder parent )
        {
            Folder folderToScan = null;
            bool foldersChanged = false;

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
                }

                CreateFileWatcher(folder);

                // Now scan the images. If there's changes it could mean the folder
                // should now be included in the folderlist, so flag it.
                ScanFolderImages( folderToScan );
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
                IndexFolder( sub, folderToScan );
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
                var missingDirs = dbChildDirs.Where(f => !new DirectoryInfo( f.Path ).IsMonitoredFolder() ).ToList();

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
        private bool ScanFolderImages(Folder folderToScan)
        {
            bool imagesWereAddedOrRemoved = false;
            int folderImageCount = 0;

            using var db = new ImageContext();
            var folder = new DirectoryInfo(folderToScan.Path);
            var allImageFiles = SafeGetImageFiles( folder );

            if (allImageFiles == null)
            {
                // Null here means we weren't able to read the contents of the directory.
                // So bail, and give up on this folder altogether.
                return false;
            }

            // First, see if images have been added or removed since we last indexed,
            // by comparing the list of known image filenames with what's on disk.
            // If they're different, we disregard the last scan date of the folder and
            // force the update. 
            bool fileListIsEqual = allImageFiles.Select(x => x.Name).ArePermutations(folderToScan.Images.Select(y => y.FileName));

            if( fileListIsEqual && folderToScan.FolderScanDate != null )
            {
                // Number of images is the same, and the folder has a scan date
                // which implies it's been scanned previously, so nothing to do.
                return true;
            }

            Logging.LogVerbose($"New or removed images in folder {folderToScan.Name}.");

            var watch = new Stopwatch("ScanFolderFiles");

            // Select just imagefiles, and most-recent first
            folderImageCount = allImageFiles.Count();

            int newImages = 0, updatedImages = 0;
            foreach (var file in allImageFiles)
            {
                try
                {
                    var dbImage = folderToScan.Images.FirstOrDefault(x => x.FileName.Equals(file.Name, StringComparison.OrdinalIgnoreCase));

                    if (dbImage != null)
                    {
                        // See if the image has changed since we last indexed it
                        bool fileChanged = file.FileIsMoreRecentThan(dbImage.LastUpdated);

                        if (!fileChanged)
                        {
                            // File hasn't changed. Look for a sidecar to see if it's been modified.
                            var sidecar = dbImage.GetSideCar();

                            if (sidecar != null)
                            {
                                // If there's a sidecar, see if that's changed.
                                fileChanged = sidecar.Filename.FileIsMoreRecentThan(dbImage.LastUpdated);
                            }
                        }

                        if (!fileChanged)
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
                        // Default the sort date to file creation date. It'll get updated
                        // later during indexing to set it to the date-taken date, if one
                        // exists.
                        image.SortDate = image.FileCreationDate.ToUniversalTime();

                        Logging.LogTrace("Adding new image {0}", image.FileName);
                        folderToScan.Images.Add(image);
                        newImages++;
                        imagesWereAddedOrRemoved = true;
                    }
                    else
                    {
                        db.Images.Update(image);
                        updatedImages++;
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogError($"Exception while scanning for new image {file}: {ex.Message}");
                }
            }

            // Now look for files to remove.
            // TODO - Sanity check that these don't hit the DB
            var filesToRemove = folderToScan.Images.Select(x => x.FileName).Except(allImageFiles.Select(x => x.Name));
            var dbImages = folderToScan.Images.Select(x => x.FileName);
            var imagesToDelete = folderToScan.Images
                                .Where(x => filesToRemove.Contains(x.FileName))
                                .ToList();

            if (imagesToDelete.Any())
            {
                imagesToDelete.ForEach(x => Logging.LogVerbose("Deleting image {0} (ID: {1})", x.FileName, x.ImageId));

                // Removing these will remove the associated ImageTag and selection references.
                db.Images.RemoveRange(imagesToDelete);

                imagesWereAddedOrRemoved = true;
            }

            // Now update the folder to say we've processed it
            folderToScan.FolderScanDate = DateTime.UtcNow;
            db.Folders.Update(folderToScan);

            db.SaveChanges("FolderScan");

            watch.Stop();

            _statusService.StatusText = string.Format("Indexed folder {0}: processed {1} images ({2} new, {3} updated, {4} removed) in {5}.",
                    folderToScan.Name, folderToScan.Images.Count(), newImages, updatedImages, imagesToDelete.Count(), watch.HumanElapsedTime);

            // Do this after we scan for images, because we only load folders if they have images.
            if (imagesWereAddedOrRemoved)
                NotifyFolderChanged();

            return imagesWereAddedOrRemoved;
        }



        /// <summary>
        /// Get all image files in a subfolder, and return them, ordered by
        /// the most recently updated first. 
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public List<FileInfo> SafeGetImageFiles(DirectoryInfo folder)
        {
            var watch = new Stopwatch("GetFiles");

            try
            {
                var files = folder.GetFiles()
                                  .Where(x => _imageProcessService.IsImageFileType(x))
                                  .OrderByDescending(x => x.LastWriteTimeUtc)
                                  .ThenByDescending(x => x.CreationTimeUtc)
                                  .ToList();

                return files;
            }
            catch (Exception ex)
            {
                Logging.LogWarning("Unable to read files from {0}: {1}", folder.FullName, ex.Message);
                return new List<FileInfo>();
            }
            finally
            {
                watch.Stop();
            }
        }

        /// <summary>
        /// Index an individual folder
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public bool IndexFolder(Folder folder)
        {
            try
            {
                return ScanFolderImages(folder);
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during IndexFolder:ScanFolderImages: {ex}");
                return false;
            }
        }

        public async Task RunMetaDataScans()
        {
            Logging.LogVerbose("Metadata scan thread starting...");

            try
            {
                using var db = new ImageContext();

                await db.GenFullText(true);

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
                                            .ThenByDescending( x => x.FileLastModDate )
                                            .Take(batchSize)
                                            .Include(x => x.Folder)
                                            .Include(x => x.MetaData)
                                            .ToArray();

                    queueQueryWatch.Stop();

                    complete = !imagesToScan.Any();

                    if (!complete)
                    {
                        var batchWatch = new Stopwatch("MetaDataBatch", 100000);
                        var writeSideCarTagsToImages = _configService.GetBool(ConfigSettings.ImportSidecarKeywords);

                        // Aggregate stuff that we'll collect up as we scan
                        var imageKeywords = new ConcurrentDictionary<Image, string[]>();

                        var newMetadataEntries = new List<ImageMetaData>();
                        var updatedEntries = new List<ImageMetaData>();
                        var updatedImages = new List<Image>();
                        var updateTimeStamp = DateTime.UtcNow;

                        foreach (var img in imagesToScan)
                        {
                            try
                            {
                                var lastWriteTime = File.GetLastWriteTimeUtc(img.FullPath);

                                if (lastWriteTime > DateTime.UtcNow.AddSeconds(-10))
                                {
                                    // If the last-write time is within 30s of now,
                                    // skip it, as it's possible it might still be
                                    // mid-copy.
                                    Logging.Log($"Skipping metadata scan for {img.FileName} - write time is too recent.");
                                    continue;
                                }

                                ImageMetaData imgMetaData = img.MetaData;

                                if (imgMetaData == null)
                                {
                                    imgMetaData = new ImageMetaData { ImageId = img.ImageId, Image = img };
                                    newMetadataEntries.Add(imgMetaData);
                                }
                                else
                                    updatedEntries.Add(imgMetaData);

                                // Scan the image from the 
                                GetImageMetaData(ref imgMetaData, out var exifKeywords);

                                // Update the timestamp
                                imgMetaData.LastUpdated = updateTimeStamp;

                                // Scan for sidecar files
                                var sideCarTags = GetSideCarKeywords(img, exifKeywords, writeSideCarTagsToImages);

                                if (sideCarTags.Any())
                                {
                                    // See if we've enabled the option to write any sidecar keywords to IPTC keywords
                                    // if they're missing in the EXIF data of the image.
                                    if (writeSideCarTagsToImages)
                                    {
                                        // Now, submit the tags; note they won't get created immediately, but in batch.
                                        Logging.LogVerbose($"Applying {sideCarTags.Count} keywords from sidecar files to image {img.FileName}");
                                        // Fire and forget this asynchronously - we don't care about waiting for it
                                        _ = _metadataService.UpdateTagsAsync(new[] { img }, sideCarTags, null);
                                    }
                                }

                                // Now combine - with case insensitivity.
                                var allKeywords = sideCarTags.Union(exifKeywords, StringComparer.OrdinalIgnoreCase);

                                // Now we have a list of all of the keywords found in the image
                                // and the sidecar.
                                if (allKeywords.Any())
                                    imageKeywords[img] = allKeywords.ToArray();

                                if (imgMetaData.DateTaken != img.SortDate)
                                {
                                    Logging.LogTrace($"Updating image {img.FileName} with DateTaken: {imgMetaData.DateTaken}.");
                                    // Always update the image sort date with the date taken,
                                    // if one was found in the metadata
                                    img.SortDate = imgMetaData.DateTaken;
                                    img.LastUpdated = updateTimeStamp;
                                    updatedImages.Add(img);
                                }
                                else
                                {
                                    if( imgMetaData.DateTaken == DateTime.MinValue )
                                        Logging.LogTrace($"Not updating image {img.FileName} with DateTaken as no valid value.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.LogError($"Exception caught during metadata scan for {img.FullPath}: {ex.Message}.");
                            }
                        }

                        var saveWatch = new Stopwatch("MetaDataSave");
                        Logging.LogVerbose($"Adding {newMetadataEntries.Count()} and updating {updatedEntries.Count()} metadata entries.");
                        await db.BulkInsert(db.ImageMetaData, newMetadataEntries);
                        await db.BulkUpdate(db.ImageMetaData, updatedEntries);

                        if (updatedImages.Any())
                        {
                            Logging.LogTrace($"Updating {updatedImages.Count()} image with new sort date.");
                            await db.BulkUpdate(db.Images, updatedImages);
                        }

                        saveWatch.Stop();

                        var tagWatch = new Stopwatch("AddTagsSave");

                        // Now save the tags
                        await AddTags( imageKeywords );

                        tagWatch.Stop();

                        batchWatch.Stop();

                        Logging.Log($"Completed metadata scan: {imagesToScan.Length} images, {newMetadataEntries.Count} added, {updatedEntries.Count} updated, {imageKeywords.Count} keywords added, in {batchWatch.HumanElapsedTime}.");
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
        private List<string> GetSideCarKeywords( Image img, string[] keywords, bool tagsWillBeWritten )
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
                    var messagePredicate = tagsWillBeWritten ? "" : "not ";
                    // Only write this log entry if we're actually going to write sidecar files.
                    Logging.Log($"Image {img.FileName} is missing {missingKeywords.Count} keywords present in the {sidecar.Type} sidecar ({sidecar.Filename.Name}). Tags will {messagePredicate}be written to images.");
                    sideCarTags = sideCarTags.Union(missingKeywords, StringComparer.OrdinalIgnoreCase).ToList();
                }
            }

            return sideCarTags;
        }

        public void StartService()
        {
            if (EnableIndexing)
            {
                Logging.Log("Starting indexing service.");

                var indexthread = new Thread(new ThreadStart(() => { ProcessIndexing(); }));
                indexthread.Name = "IndexingThread";
                indexthread.IsBackground = true;
                indexthread.Priority = ThreadPriority.Lowest;
                indexthread.Start();

                Task.Run(() => RunMetaDataScans());
            }
            else
                Logging.Log("Indexing service has been disabled.");
        }

        private void ProcessIndexing()
        {
            try
            {
                RunIndexing();
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception in RunIndexing: {ex.Message}");
            }
        }

        public void PerformFullIndex()
        {
            // Perform a full index at startup
            _statusService.StatusText = "Full Indexing starting...";
            var root = new DirectoryInfo(RootFolder);

            var watch = new Stopwatch("CompleteIndex", -1);

            try
            {
                IndexFolder(root, null);
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during full indexing: {ex}");
            }
            watch.Stop();

            _statusService.StatusText = "Full Indexing Complete.";
        }

        /// <summary>
        /// Marks the FolderScanDate as null, which will cause the 
        /// indexing service to pick it up and scan it for any changes. 
        /// </summary>
        /// <param name="folders"></param>
        /// <returns></returns>
        public async Task FlagFoldersForRescan( IEnumerable<Folder> folders )
        {
            using var db = new ImageContext();

            var updatedFolders = folders.ToList();

            if (updatedFolders.Count == 1)
                Logging.Log($"Flagged folder {updatedFolders.First().Path} for re-scan...");
            else
                Logging.Log($"Flagged {updatedFolders.Count} folders for re-scan...");

            updatedFolders.ForEach(x => x.FolderScanDate = null);
            await db.BulkUpdate( db.Folders, updatedFolders);
        }

        private void RunIndexing()
        {
            LoadTagCache();

            // We always perform a full index at startup. This checks the
            // state of the folders/images, and also creates the filewatchers
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
                    var pendingFolders = db.Folders.Where(f => folders.Contains(f.Path));

                    FlagFoldersForRescan(pendingFolders).GetAwaiter().GetResult();
                }

                // Now, see if there's any folders that have a null scan date.
                var foldersToIndex = db.Folders.Where(x => x.FolderScanDate == null)
                                               .Take( batchSize )
                                               .ToList();

                if( foldersToIndex.Any() )
                {
                    _statusService.StatusText = $"Detected {foldersToIndex.Count()} folders with new/changed images.";

                    foreach ( var folder in foldersToIndex )
                    {
                        var dir = new DirectoryInfo(folder.Path);
                        // Scan the folder for subdirs
                        IndexFolder(dir, null);
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
        private void EnqueueFolderChangeForRescan( FileInfo file, WatcherChangeTypes changeType )
        {
            using var db = new ImageContext();

            var folder = file.Directory.FullName;

            // If it's hidden, or already in the queue, ignore it.
            if (file.IsHidden() || folderQueue.Contains(folder))
                return;

            // Ignore non images, and hidden files/folders.
            if (file.IsDirectory() || _imageProcessService.IsImageFileType(file) || file.IsSidecarFileType() )
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

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Logging.LogVerbose($"FileWatcher: {e.FullPath} {e.ChangeType}");

            var file = new FileInfo(e.FullPath);

            EnqueueFolderChangeForRescan(file, e.ChangeType);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            Logging.LogVerbose($"FileWatcher: {e.OldFullPath} => {e.FullPath} {e.ChangeType}");

            var oldfile = new FileInfo(e.OldFullPath);
            var newfile = new FileInfo(e.FullPath);

            EnqueueFolderChangeForRescan(oldfile, e.ChangeType);
            EnqueueFolderChangeForRescan(newfile, e.ChangeType);
        }

        public async Task MarkFolderForScan( Folder folder )
        {
            using var db = new ImageContext();

            // TODO: Abstract this once EFCore Bulkextensions work in efcore 6
            await db.Database.ExecuteSqlInterpolatedAsync($"Update imagemetadata Set Lastupdated = null where imageid in (select imageid from folders where folderid = {folder.FolderId})");
        }

        public async Task MarkImageForScan(Image image)
        {
            using var db = new ImageContext();

            // TODO: Abstract this once EFCore Bulkextensions work in efcore 6
            await db.Database.ExecuteSqlInterpolatedAsync($"Update imagemetadata Set Lastupdated = null where imageid = {image.ImageId}");
        }
    }
}
