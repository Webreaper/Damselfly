using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DBAbstractions;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Shared.Utils;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Xmp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Directory = MetadataExtractor.Directory;
using Stopwatch = Damselfly.Shared.Utils.Stopwatch;
using Tag = Damselfly.Core.Models.Tag;

namespace Damselfly.Core.Services;

public class MetaDataService : IProcessJobFactory, ITagSearchService, IRescanProvider
{
    private static readonly DateTime NoMetadataDate = DateTime.MinValue;
    private readonly ConfigService _configService;
    private readonly ExifService _exifService;
    private readonly ImageCache _imageCache;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly IStatusService _statusService;
    private readonly WorkService _workService;
    private IDictionary<string, Camera> _cameraCache;

    private IDictionary<string, Lens> _lensCache;

    // Some caching to avoid repeatedly reading tags, cameras and lenses
    // from the DB.
    private IDictionary<string, Tag> _tagCache;

    public MetaDataService(IServiceScopeFactory scopeFactory, IStatusService statusService, ExifService exifService,
        ConfigService config, ImageCache imageCache, WorkService workService)
    {
        _scopeFactory = scopeFactory;
        _statusService = statusService;
        _configService = config;
        _exifService = exifService;
        _imageCache = imageCache;
        _workService = workService;
    }

    public ICollection<Camera> Cameras
    {
        get { return _cameraCache.Values.OrderBy(x => x.Make).ThenBy(x => x.Model).ToList(); }
    }

    public ICollection<Lens> Lenses
    {
        get { return _lensCache.Values.OrderBy(x => x.Make).ThenBy(x => x.Model).ToList(); }
    }

    public IEnumerable<Tag> CachedTags => _tagCache.Values.ToList();

    public JobPriorities Priority => JobPriorities.Metadata;

    public async Task<ICollection<IProcessJob>> GetPendingJobs(int maxJobs)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        // Find all images where there's either no metadata, or where the image or sidecar file 
        // was updated more recently than the image metadata
        var imageIds = await db.Images.Where(x => x.MetaData == null ||
                                                  x.LastUpdated > x.MetaData.LastUpdated)
            .OrderByDescending(x => x.LastUpdated)
            .ThenByDescending(x => x.FileLastModDate)
            .Take(maxJobs)
            .Select(x => x.ImageId)
            .ToListAsync();

        var jobs = imageIds.Select(x => new MetadataProcess { ImageId = x, Service = this })
            .ToArray();

        return jobs;
    }

    public async Task MarkFolderForScan(int folderId)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        //var queryable = db.ImageMetaData.Where(img => img.Image.FolderId == folder.FolderId);
        //int updated = await db.BatchUpdate(queryable, x => new ImageMetaData { LastUpdated = NoMetadataDate });

        var updated =
            await ImageContext.UpdateMetadataFields(db, folderId, "LastUpdated", $"'{NoMetadataDate:yyyy-MM-dd}'");

        if ( updated != 0 )
            _statusService.UpdateStatus($"{updated} images in folder flagged for Metadata scanning.");

        _workService.FlagNewJobs(this);
    }

    public async Task MarkAllForScan()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var updated =
            await db.BatchUpdate(db.ImageMetaData, i => i.SetProperty(x => x.LastUpdated, x => NoMetadataDate));

        _statusService.UpdateStatus($"All {updated} images flagged for Metadata scanning.");

        _workService.FlagNewJobs(this);
    }

    public async Task MarkImagesForScan(ICollection<int> images)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var queryable = db.ImageMetaData.Where(i => images.Contains(i.ImageId));

        var rows = await db.BatchUpdate(queryable, i => i.SetProperty(x => x.LastUpdated, x => NoMetadataDate));

        var msgText = rows == 1 ? "Image" : $"{rows} images";
        _statusService.UpdateStatus($"{msgText} flagged for Metadata scanning.");
    }

    /// <summary>
    ///     Search for a set of tags - used for autocomplete.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public Task<ICollection<Tag>> GetAllTags()
    {
        ICollection<Tag> result = CachedTags.ToList();
        return Task.FromResult(result);
    }

    /// <summary>
    ///     Search for a set of tags - used for autocomplete.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public Task<ICollection<Tag>> SearchTags(string text)
    {
        var results = new List<Tag>();
        var searchText = text.Trim();

        // Only query the DB if we have more than 2 chars.
        if ( searchText.Length > 1 )
        {
            // We include any keyword that contains the search term - but exclude 
            // the actual search term, as we'll add it explicitly at the top of the list.
            var tags = CachedTags
                .Where(x => x.Keyword.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                            && !x.Keyword.Equals(searchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Favourite ? 0 : 1) // Favourites first
                .ThenBy(x => x.Keyword) // Then order alphabetically
                .Take(30); // Don't go mad with the number we return

            results.AddRange(tags);
        }

        results.Insert(0, new Tag { Keyword = text });

        return Task.FromResult((ICollection<Tag>)results);
    }

    public void StartService()
    {
        InitCameraAndLensCaches();
        LoadTagCache();

        _workService.AddJobSource(this);
    }

    /// <summary>
    ///     Read the metadata, and handle any exceptions.
    /// </summary>
    /// <param name="imagePath"></param>
    /// <returns>Metadata, or Null if there was an error</returns>
    private IReadOnlyList<Directory> SafeReadImageMetadata(string imagePath)
    {
        var watch = new Stopwatch("ReadMetaData");

        IReadOnlyList<Directory> metadata = null;

        if ( File.Exists(imagePath) )
            try
            {
                metadata = ImageMetadataReader.ReadMetadata(imagePath);
            }
            catch ( ImageProcessingException ex )
            {
                Logging.Log("Metadata read for image {0}: {1}", imagePath, ex.Message);
            }
            catch ( IOException ex )
            {
                Logging.Log("File error reading metadata for {0}: {1}", imagePath, ex.Message);
            }

        watch.Stop();

        return metadata;
    }

    /// <summary>
    ///     Pull out the XMP face area so we can convert it to a real face in the DB
    /// </summary>
    /// <param name="xmpDirectory"></param>
    /// <returns></returns>
    private List<ImageObject> ReadXMPFaceRegionData(XmpDirectory xmpDirectory, Image image, string orientation)
    {
        try
        {
            var newFaces = new List<ImageObject>();

            var nvps = xmpDirectory.XmpMeta.Properties
                .Where(x => !string.IsNullOrEmpty(x.Path))
                .ToDictionary(x => x.Path, y => y.Value);

            var iRegion = 0;
            var (flipH, flipV, switchOrient) = FlipHorizVert(orientation);

            while ( true )
            {
                iRegion++;

                var regionBase = $"mwg-rs:Regions/mwg-rs:RegionList[{iRegion}]/mwg-rs:";

                // Check if there's a name for the next region. If not, we've probably done them all
                if ( !nvps.ContainsKey($"{regionBase}Name") )
                    break;

                var name = nvps[$"{regionBase}Name"];
                var type = nvps[$"{regionBase}Type"];
                var xStr = nvps[$"{regionBase}Area/stArea:x"];
                var yStr = nvps[$"{regionBase}Area/stArea:y"];
                var wStr = nvps[$"{regionBase}Area/stArea:w"];
                var hStr = nvps[$"{regionBase}Area/stArea:h"];

                var x = Convert.ToDouble(xStr);
                var y = Convert.ToDouble(yStr);
                var w = Convert.ToDouble(wStr);
                var h = Convert.ToDouble(hStr);

                if ( switchOrient )
                {
                    var xTemp = y;
                    y = x;
                    x = xTemp;
                }

                if ( flipH )
                    x = 1 - x;

                if ( flipV )
                    y = 1 - y;

                var newPerson = new Person
                {
                    Name = name,
                    LastUpdated = DateTime.UtcNow,
                    State = Person.PersonState.Identified
                };

                var newFace = new ImageObject
                {
                    RecogntionSource = ImageObject.RecognitionType.ExternalApp,
                    ImageId = image.ImageId,
                    // Note that x and y in this case are the centrepoints of the faces. 
                    // Make sure we offset the rects by half their width and height
                    // to centre them on the face.
                    RectX = (int)((x - w / 2) * image.MetaData.Width),
                    RectY = (int)((y - h / 2) * image.MetaData.Height),
                    RectHeight = (int)(h * image.MetaData.Height),
                    RectWidth = (int)(w * image.MetaData.Width),
                    TagId = 0,
                    Type = ImageObject.ObjectTypes.Face.ToString(),
                    Score = 100,
                    Person = newPerson
                };

                newFaces.Add(newFace);
            }

            return newFaces;
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception while parsing XMP face/region data: {ex}");
        }

        return new List<ImageObject>();
    }

    /// <summary>
    ///     Scans an image file on disk for its metadata, using the MetaDataExtractor
    ///     library. The image object is populated with the metadata, and the IPTC
    ///     keywords are returned back to the caller for processing and ingestion
    ///     into the DB.
    /// </summary>
    /// <param name="image">Image object, which will be updated with metadata</param>
    /// <param name="keywords">Array of keyword tags in the image EXIF data</param>
    /// <param name="newFaces">Array of face objects in the XMP metadata</param>
    private bool GetImageMetaData(ref ImageMetaData imgMetaData, out string[] keywords, out List<ImageObject> newFaces)
    {
        var metaDataReadSuccess = false;
        var image = imgMetaData.Image;
        keywords = new string[0];
        newFaces = null;

        try
        {
            var metadata = SafeReadImageMetadata(image.FullPath);

            if ( metadata != null )
            {
                metaDataReadSuccess = true;

                var subIfdDirectory = metadata.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                if ( subIfdDirectory != null )
                {
                    imgMetaData.DateTaken = subIfdDirectory.SafeGetExifDateTime(ExifDirectoryBase.TagDateTimeDigitized);

                    if ( imgMetaData.DateTaken == DateTime.MinValue )
                        imgMetaData.DateTaken =
                            subIfdDirectory.SafeGetExifDateTime(ExifDirectoryBase.TagDateTimeOriginal);

                    imgMetaData.Height = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagExifImageHeight);
                    imgMetaData.Width = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagExifImageWidth);

                    if ( imgMetaData.Width == 0 )
                        imgMetaData.Width = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagImageWidth);
                    if ( imgMetaData.Height == 0 )
                        imgMetaData.Height = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagImageHeight);

                    imgMetaData.ISO = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagIsoEquivalent);
                    imgMetaData.FNum = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagFNumber);
                    imgMetaData.Exposure = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagExposureTime);
                    imgMetaData.Rating = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagRating);

                    var lensMake = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagLensMake);
                    var lensModel = subIfdDirectory.SafeExifGetString("Lens Model");
                    var lensSerial = subIfdDirectory.SafeExifGetString(ExifDirectoryBase.TagLensSerialNumber);

                    // If there was no lens make/model, it may be because it's in the Makernotes. So attempt
                    // to extract it. This code definitely works for a Leica Panasonic lens on a Panasonic body.
                    // It may not work for other things.
                    if ( string.IsNullOrEmpty(lensMake) || string.IsNullOrEmpty(lensModel) )
                    {
                        var makerNoteDir = metadata.FirstOrDefault(x =>
                            x.Name.Contains("Makernote", StringComparison.OrdinalIgnoreCase));
                        if ( makerNoteDir != null )
                            if ( string.IsNullOrEmpty(lensModel) )
                                lensModel = makerNoteDir.SafeExifGetString("Lens Type");
                    }

                    if ( !string.IsNullOrEmpty(lensMake) || !string.IsNullOrEmpty(lensModel) )
                    {
                        if ( string.IsNullOrEmpty(lensModel) || lensModel == "N/A" )
                            lensModel = "Generic " + lensMake;

                        imgMetaData.LensId = GetLens(lensMake, lensModel, lensSerial).LensId;
                    }

                    var flash = subIfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagFlash);

                    imgMetaData.FlashFired = (flash & 0x1) != 0x0;
                }

                var jpegDirectory = metadata.OfType<JpegDirectory>().FirstOrDefault();

                if ( jpegDirectory != null )
                {
                    if ( imgMetaData.Width == 0 )
                        imgMetaData.Width = jpegDirectory.SafeGetExifInt(JpegDirectory.TagImageWidth);
                    if ( imgMetaData.Height == 0 )
                        imgMetaData.Height = jpegDirectory.SafeGetExifInt(JpegDirectory.TagImageHeight);
                }

                var gpsDirectory = metadata.OfType<GpsDirectory>().FirstOrDefault();

                if ( gpsDirectory != null )
                {
                    var location = gpsDirectory.GetGeoLocation();

                    if ( location != null )
                    {
                        imgMetaData.Longitude = location.Longitude;
                        imgMetaData.Latitude = location.Latitude;
                    }
                }

                var orientation = "1"; // Default
                var IfdDirectory = metadata.OfType<ExifIfd0Directory>().FirstOrDefault();

                if ( IfdDirectory != null )
                {
                    var exifDesc = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagImageDescription).SafeTrim();
                    imgMetaData.Description = FilteredDescription(exifDesc);

                    imgMetaData.Copyright = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagCopyright).SafeTrim();

                    orientation = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagOrientation);
                    var camMake = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagMake);
                    var camModel = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagModel);
                    var camSerial = IfdDirectory.SafeExifGetString(ExifDirectoryBase.TagBodySerialNumber);
                    imgMetaData.Rating = IfdDirectory.SafeGetExifInt(ExifDirectoryBase.TagRating);

                    if ( !string.IsNullOrEmpty(camMake) || !string.IsNullOrEmpty(camModel) )
                        imgMetaData.CameraId = GetCamera(camMake, camModel, camSerial).CameraId;

                    if ( NeedToSwitchWidthAndHeight(orientation) )
                    {
                        // It's orientated rotated. So switch the height and width
                        var temp = imgMetaData.Width;
                        imgMetaData.Width = imgMetaData.Height;
                        imgMetaData.Height = temp;
                    }
                }

                var IPTCdir = metadata.OfType<IptcDirectory>().FirstOrDefault();

                if ( IPTCdir != null )
                {
                    var caption = IPTCdir.SafeExifGetString(IptcDirectory.TagCaption).SafeTrim();
                    var byline = IPTCdir.SafeExifGetString(IptcDirectory.TagByLine).SafeTrim();
                    var source = IPTCdir.SafeExifGetString(IptcDirectory.TagSource).SafeTrim();

                    imgMetaData.Caption = FilteredDescription(caption);
                    if ( !string.IsNullOrEmpty(imgMetaData.Copyright) )
                        imgMetaData.Copyright = IPTCdir.SafeExifGetString(IptcDirectory.TagCopyrightNotice).SafeTrim();
                    imgMetaData.Credit = IPTCdir.SafeExifGetString(IptcDirectory.TagCredit).SafeTrim();

                    if ( string.IsNullOrEmpty(imgMetaData.Credit) && !string.IsNullOrEmpty(source) )
                        imgMetaData.Credit = source;

                    if ( !string.IsNullOrEmpty(byline) )
                    {
                        if ( !string.IsNullOrEmpty(imgMetaData.Credit) )
                            imgMetaData.Credit += $" ({byline})";
                        else
                            imgMetaData.Credit += $"{byline}";
                    }

                    // Stash the keywords in the dict, they'll be stored later.
                    var keywordList = IPTCdir?.GetStringArray(IptcDirectory.TagKeywords);
                    if ( keywordList != null )
                        keywords = keywordList;
                }

                var xmpDirectory = metadata.OfType<XmpDirectory>().FirstOrDefault();

                if ( xmpDirectory != null ) newFaces = ReadXMPFaceRegionData(xmpDirectory, image, orientation);
            }

            if ( imgMetaData.Width != 0 && imgMetaData.Height != 0 )
                imgMetaData.AspectRatio = imgMetaData.Width / (double)imgMetaData.Height;

            var keywordsSummary = keywords.Any() ? $", found {keywords.Count()} keywords." : string.Empty;
            Logging.Log($"Read metadata for {image.FullPath} (ID: {image.ImageId}) {keywordsSummary}");

            DumpMetaData(image, metadata);
        }
        catch ( Exception ex )
        {
            Logging.Log("Error reading image metadata for {0}: {1}", image.FullPath, ex.Message);
            metaDataReadSuccess = false;
        }

        return metaDataReadSuccess;
    }

    private string FilteredDescription(string desc)
    {
        if ( !string.IsNullOrEmpty(desc) )
            // No point clogging up the DB with thousands
            // of identical default descriptions
            if ( desc.Trim().Equals("OLYMPUS DIGITAL CAMERA") )
                return string.Empty;

        return desc;
    }

    /// <summary>
    ///     Scan the metadata for an image - including the EXIF data, keywords
    ///     and any XMP/ON1 sidecars. Then the metadata is written to the DB.
    /// </summary>
    /// <param name="imageId"></param>
    /// <returns></returns>
    public async Task ScanMetaData(int imageId)
    {
        var watch = new Stopwatch("ScanMetadata");

        var writeSideCarTagsToImages = _configService.GetBool(ConfigSettings.ImportSidecarKeywords);

        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var updateTimeStamp = DateTime.UtcNow;
        var imageKeywords = new List<string>();
        var sideCarTags = new List<string>();
        List<ImageObject> xmpFaces = null;

        var img = await _imageCache.GetCachedImage(imageId);

        db.Attach(img);

        try
        {
            var lastWriteTime = File.GetLastWriteTimeUtc(img.FullPath);

            if (lastWriteTime < DateTime.UtcNow.AddMinutes( 1 ) && lastWriteTime > DateTime.UtcNow.AddSeconds(-10))
            {
                // If the last-write time is within 30s of now, but it's not a time far in the future
                // we skip it, as it's possible it might still be mid-copy.
                // TODO: We need a better way of managing this
                Logging.LogWarning($"Skipping metadata scan for {img.FileName} - write time is too recent.");
                return;
            }

            var imgMetaData = img.MetaData;

            if ( imgMetaData == null )
            {
                imgMetaData = new ImageMetaData { ImageId = img.ImageId, Image = img };
                img.MetaData = imgMetaData;
                db.ImageMetaData.Add(imgMetaData);
            }
            else
            {
                db.ImageMetaData.Update(imgMetaData);
            }

            // Update the timestamp regardless of whether we succeeded to read the metadata
            imgMetaData.LastUpdated = updateTimeStamp;

            // Scan the image from the 
            if ( GetImageMetaData(ref imgMetaData, out var exifKeywords, out xmpFaces) )
            {
                // Scan for sidecar files
                sideCarTags = GetSideCarKeywords(img, exifKeywords, writeSideCarTagsToImages);

                imageKeywords = sideCarTags.Union(exifKeywords, StringComparer.OrdinalIgnoreCase).ToList();
            }

            if ( imgMetaData.DateTaken != img.SortDate )
            {
                Logging.LogTrace($"Updating image {img.FileName} with DateTaken: {imgMetaData.DateTaken}.");
                // Always update the image sort date with the date taken,
                // if one was found in the metadata
                if ( imgMetaData.DateTaken > DateTime.MinValue )
                    // Don't update the date to date taken if the one there isn't valid
                    img.SortDate = imgMetaData.DateTaken;

                img.LastUpdated = updateTimeStamp;
                db.Images.Update(img);
            }
            else
            {
                if ( imgMetaData.DateTaken == DateTime.MinValue )
                    Logging.LogTrace($"Not updating image {img.FileName} with DateTaken as no valid value.");
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception caught during metadata scan for {img.FullPath}: {ex.Message}.");
        }
        finally
        {
            // Ensure we update the timestamp for the item
            var changesSaved = await db.SaveChangesAsync("ImageMetaDataSave");

            if ( changesSaved == 0 )
                Logging.LogError($"No changes saved after metadata scan for image {img.ImageId}");
        }

        // Now save the tags
        var tagsAdded = await WriteTagsForImage(img, imageKeywords);

        await WriteXMPFaces(img, xmpFaces);

        _imageCache.Evict(imageId);

        watch.Stop();

        if ( sideCarTags.Any() && writeSideCarTagsToImages )
        {
            // If we've enabled the option to write any sidecar keywords to IPTC
            // keywords if they're missing in the EXIF data of the image submit
            // the tags; note they won't get created immediately, but in batch.
            Logging.LogVerbose($"Applying {sideCarTags.Count} keywords from sidecar files to image {img.FileName}");

            // Fire and forget this asynchronously - we don't care about waiting for it
            _ = _exifService.UpdateTagsAsync(img, sideCarTags);
        }
    }

    /// <summary>
    ///     Write the face records for any faces found in the XMP Region metadata
    /// </summary>
    /// <param name="xmpFaces"></param>
    /// <returns></returns>
    private async Task WriteXMPFaces(Image image, List<ImageObject> xmpFaces)
    {
        if ( xmpFaces != null && xmpFaces.Any() )
            try
            {
                var createdTags = await CreateTagsFromStrings(new[] { "Face" });
                var faceTag = createdTags.First();

                // Find existing faces and swap if appropriate
                var names = xmpFaces.Select(x => x.Person.Name)
                    .ToList();

                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetService<ImageContext>();

                var peopleLookup = db.People.Where(x => names.Contains(x.Name))
                    .ToDictionary(x => x.Name, y => y.PersonId);

                foreach ( var xmpFace in xmpFaces )
                {
                    xmpFace.TagId = faceTag.TagId;

                    if ( peopleLookup.TryGetValue(xmpFace.Person.Name, out var matchedPersonId) )
                    {
                        xmpFace.Person = null;
                        xmpFace.PersonId = matchedPersonId;
                    }

                    db.ImageObjects.Add(xmpFace);
                }

                // Delete any objects that we found in the metadata before, so we start afresh.
                await db.BatchDelete(db.ImageObjects
                    .Where(x => x.ImageId.Equals(image.ImageId) &&
                                x.RecogntionSource == ImageObject.RecognitionType.ExternalApp));

                // TODO - check for existing rects/faces and replace
                await db.SaveChangesAsync("SaveFacesMetaData");
            }
            catch ( Exception ex )
            {
                Logging.LogError($"Exception while processing XMP faces: {ex}");
            }
    }

    /// <summary>
    ///     Given a collection of images and their keywords, performs a bulk insert
    ///     of them all. This is way more performant than adding the keywords as
    ///     each image is indexed, and allows us to bulk-update the freetext search
    ///     too.
    /// </summary>
    /// <param name="imageKeywords"></param>
    /// <param name="type"></param>
    private async Task<int> WriteTagsForImage(Image image, List<string> imageKeywords)
    {
        var tagsAdded = 0;
        var watch = new Stopwatch("WriteTagsForImage");

        if ( BaseDBModel.ReadOnly )
            return tagsAdded;

        try
        {
            // First, find all the distinct keywords, and check whether
            // they're in the cache. If not, create them in the DB.
            await CreateTagsFromStrings(imageKeywords);
        }
        catch ( Exception ex )
        {
            Logging.LogError("Exception adding Tags: {0}", ex);
        }

        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();
        using var transaction = db.Database.BeginTransaction();

        try
        {
            // Create the new tag objects, pulling the tags from the cache
            var newImageTags = imageKeywords.Select(keyword => new ImageTag
                {
                    ImageId = image.ImageId,
                    TagId = _tagCache[keyword].TagId
                })
                .ToList();

            Logging.LogTrace($"Updating {newImageTags.Count()} ImageTags");

            // First, get the image tags for the image
            var existingImageTagIds = image.ImageTags.Select(x => x.TagId).ToList();

            // Figure out which tags are new, and which existing tags need to be removed.
            var toDelete = existingImageTagIds.Where(id => !newImageTags.Select(x => x.TagId).Contains(id)).ToList();
            var toAdd = newImageTags.Where(x => !existingImageTagIds.Contains(x.TagId)).ToList();

            if ( toDelete.Any() )
            {
                var delWatch = new Stopwatch("AddTagsDelete");
                await db.BatchDelete(db.ImageTags.Where(y => y.ImageId == image.ImageId && toDelete.Contains(y.TagId)));
                delWatch.Stop();
            }

            if ( toAdd.Any() )
            {
                var addWatch = new Stopwatch("AddTagsInsert");
                await db.BulkInsert(db.ImageTags, toAdd);
                
                addWatch.Stop();
            }

            transaction.Commit();
            tagsAdded = newImageTags.Count;
        }
        catch ( Exception ex )
        {
            Logging.LogError("Exception adding ImageTags: {0}", ex);
        }

        watch.Stop();

        return tagsAdded;
    }

    /// <summary>
    ///     Some image editing apps such as Lightroom, On1, etc., do not persist the keyword metadata
    ///     in the images by default. This can mean you keyword-tag them, but those keywords are only
    ///     stored in the sidecars. Damselfly only scans keyword metadata from the EXIF image data
    ///     itself.
    ///     So to rectify this, we can either read the sidecar files for those keywords, and optionally
    ///     write the missing keywords to the Exif Metadata as we index them.
    /// </summary>
    /// <param name="img"></param>
    /// <param name="keywords"></param>
    private List<string> GetSideCarKeywords(Image img, string[] keywords, bool tagsWillBeWritten)
    {
        var watch = new Stopwatch("GetSideCarKeywords");

        var sideCarTags = new List<string>();

        var sidecar = img.GetSideCar();

        if ( sidecar != null )
        {
            // We need to be really careful here, to discount unicode-encoding differences, because otherwise
            // we get into an infinite loop where we write one string to the KeywordOperations table, it gets
            // picked up by the ExifService, written to the image using ExifTool - but with slightly different
            // character encoding - and then the next time we come through here and check, the keywords look
            // different. Rinse and repeat. :-s
            var imageKeywords = keywords.Select(x => x.Sanitise());
            var sidecarKeywords = sidecar.GetKeywords().Select(x => x.Sanitise());

            var missingKeywords = sidecarKeywords
                .Except(imageKeywords, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if ( missingKeywords.Any() )
            {
                var messagePredicate = tagsWillBeWritten ? "" : "not ";
                // Only write this log entry if we're actually going to write sidecar files.
                Logging.Log(
                    $"Image {img.FileName} is missing {missingKeywords.Count} keywords present in the {sidecar.Type} sidecar ({sidecar.Filename.Name}). Tags will {messagePredicate}be written to images.");
                sideCarTags = sideCarTags.Union(missingKeywords, StringComparer.OrdinalIgnoreCase).ToList();
            }
        }

        watch.Stop();

        return sideCarTags;
    }

    /// <summary>
    ///     These are the orientation strings:
    ///     "Top, left side (Horizontal / normal)",
    ///     "Top, right side (Mirror horizontal)",
    ///     "Bottom, right side (Rotate 180)",
    ///     "Bottom, left side (Mirror vertical)",
    ///     "Left side, top (Mirror horizontal and rotate 270 CW)",
    ///     "Right side, top (Rotate 90 CW)",
    ///     "Right side, bottom (Mirror horizontal and rotate 90 CW)",
    ///     "Left side, bottom (Rotate 270 CW)"
    /// </summary>
    /// <param name="orientation"></param>
    /// <returns></returns>
    private bool NeedToSwitchWidthAndHeight(string orientation)
    {
        return orientation switch
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
    }

    /// <summary>
    ///     See NeedToSwitchWidthAndHeight for the states
    ///     1 = 0 degrees: the correct orientation, no adjustment is required.
    ///     2 = 0 degrees, mirrored: image has been flipped back-to-front.
    ///     3 = 180 degrees: image is upside down.
    ///     4 = 180 degrees, mirrored: image has been flipped back-to-front and is upside down.
    ///     5 = 90 degrees: image has been flipped back-to-front and is on its side.
    ///     6 = 90 degrees, mirrored: image is on its side.
    ///     7 = 270 degrees: image has been flipped back-to-front and is on its far side.
    ///     8 = 270 degrees, mirrored: image is on its far side.
    /// </summary>
    /// <param name="orientation"></param>
    /// <returns>
    ///     Three bools: whether the h value should be flipped, whether the w value should be flipped and whether both
    ///     should be swapped
    /// </returns>
    private (bool, bool, bool) FlipHorizVert(string orientation)
    {
        return orientation switch
        {
            "3" => (true, true, false),
            "Top, right side (Mirror horizontal)" => (true, false, false),
            "Bottom, right side (Rotate 180)" => (true, true, false),
            "Bottom, left side (Mirror vertical)" => (false, true, false),
            "6" => (false, false, true),

            // TODO: Guessing these, but worth a shot. :)
            "4" => (false, false, true),
            "8" => (false, false, true),
            _ => (false, false, false)
        };
    }

    /// <summary>
    ///     Dump metadata out in tracemode.
    /// </summary>
    /// <param name="metadata"></param>
    private void DumpMetaData(Image img, IReadOnlyList<Directory> metadata)
    {
        if ( !Debugger.IsAttached )
            return;

        Logging.Log($"Metadata dump for: {img.FileName}:");
        foreach ( var dir in metadata )
        {
            Logging.Log($" Directory: {dir.Name}:");

            if ( dir is XmpDirectory )
            {
                var xmpDirectory = dir as XmpDirectory;

                foreach ( var property in xmpDirectory.XmpMeta.Properties )
                    if ( !string.IsNullOrEmpty(property.Value) )
                        Logging.Log($"  Tag: {property.Path} = {property.Value}");
            }
            else
            {
                foreach ( var tag in dir.Tags ) Logging.Log($"  Tag: {tag.Name} = {tag.Description}");
            }
        }
    }

    public class MetadataProcess : IProcessJob
    {
        public int ImageId { get; set; }
        public MetaDataService Service { get; set; }
        public bool CanProcess => true;
        public string Name => "Metadata scan";
        public string Description => $"{Name} for ID: {ImageId}";
        public JobPriorities Priority => JobPriorities.Metadata;

        public async Task Process()
        {
            await Service.ScanMetaData(ImageId);
        }

        public override string ToString()
        {
            return Description;
        }
    }

    #region Tag, Lens and Camera Caching

    private void InitCameraAndLensCaches()
    {
        if ( _lensCache == null )
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetService<ImageContext>();

            _lensCache = new ConcurrentDictionary<string, Lens>(db.Lenses
                .AsNoTracking()
                .ToDictionary(x => x.Make + x.Model, y => y));
        }

        if ( _cameraCache == null )
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetService<ImageContext>();

            _cameraCache = new ConcurrentDictionary<string, Camera>(db.Cameras
                .AsNoTracking() // We never update, so this is faster
                .ToDictionary(x => x.Make + x.Model, y => y));
        }
    }

    /// <summary>
    ///     Get a camera object, for each make/model. Uses an in-memory cache for speed.
    /// </summary>
    /// <param name="make"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    private Camera GetCamera(string make, string model, string serial)
    {
        var cacheKey = make + model;

        if ( string.IsNullOrEmpty(cacheKey) )
            return null;

        if ( !_cameraCache.TryGetValue(cacheKey, out var cam) )
        {
            // It's a new one.
            cam = new Camera { Make = make, Model = model, Serial = serial };

            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetService<ImageContext>();

            db.Cameras.Add(cam);
            db.SaveChanges("SaveCamera");

            _cameraCache[cacheKey] = cam;
        }

        return cam;
    }

    /// <summary>
    ///     Get a lens object, for each make/model. Uses an in-memory cache for speed.
    /// </summary>
    /// <param name="make"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    private Lens GetLens(string make, string model, string serial)
    {
        var cacheKey = make + model;

        if ( string.IsNullOrEmpty(cacheKey) )
            return null;

        if ( !_lensCache.TryGetValue(cacheKey, out var lens) )
        {
            // It's a new one.
            lens = new Lens { Make = make, Model = model, Serial = serial };

            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetService<ImageContext>();

            db.Lenses.Add(lens);
            db.SaveChanges("SaveLens");

            _lensCache[cacheKey] = lens;
        }

        return lens;
    }

    /// <summary>
    ///     Initialise the in-memory cache of tags.
    /// </summary>
    /// <param name="force"></param>
    private void LoadTagCache(bool force = false)
    {
        try
        {
            if ( _tagCache == null || force )
            {
                var watch = new Stopwatch("LoadTagCache");

                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetService<ImageContext>();

                // Pre-cache tags from DB.
                _tagCache = new ConcurrentDictionary<string, Tag>(db.Tags
                    .AsNoTracking()
                    .ToDictionary(k => k.Keyword, v => v));
                if ( _tagCache.Any() )
                    Logging.LogTrace("Pre-loaded cach with {0} tags.", _tagCache.Count());

                watch.Stop();
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Unexpected exception loading tag cache: {ex.Message}");
        }
    }

    /// <summary>
    ///     Return a tag by its ID.
    ///     TODO: Is this faster, or slower than a DB query, given it means iterating
    ///     a collection of, say, 10,000 tags. Probably faster, but perhaps we should
    ///     maintain a dict of ID => tag?
    /// </summary>
    /// <param name="tagId"></param>
    /// <returns></returns>
    public Task<Tag> GetTag(int tagId)
    {
        var tag = _tagCache.Values.FirstOrDefault(x => x.TagId == tagId);

        return Task.FromResult(tag);
    }

    public Tag GetTag(string keyword)
    {
        // TODO: Should we make the tag-cache key case-insensitive? What would happen?!
        var tag = _tagCache.Values.Where(x => x.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        return tag;
    }

    public async Task<List<Tag>> CreateTagsFromStrings(IEnumerable<string> tags)
    {
        var watch = new Stopwatch("CreateTagsFromStrings");

        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        // Find the tags that aren't already in the cache
        var newTags = tags.Distinct().Where(x => !_tagCache.ContainsKey(x))
            .Select(x => new Tag { Keyword = x, TagType = Tag.TagTypes.IPTC })
            .ToList();


        if ( newTags.Any() )
        {
            Logging.LogTrace("Adding {0} tags", newTags.Count());

            await db.BulkInsert(db.Tags, newTags);

            // Add the new items to the cache. 
            newTags.ForEach(x => _tagCache[x.Keyword] = x);
        }

        var allTags = tags.Select(x => _tagCache[x]).ToList();

        watch.Stop();

        return allTags;
    }

    public static void GetImageSize(string fullPath, out int width, out int height)
    {
        IReadOnlyList<Directory> metadata;

        width = height = 0;
        metadata = ImageMetadataReader.ReadMetadata(fullPath);

        var jpegDirectory = metadata.OfType<JpegDirectory>().FirstOrDefault();

        if ( jpegDirectory != null )
        {
            width = jpegDirectory.SafeGetExifInt(JpegDirectory.TagImageWidth);
            height = jpegDirectory.SafeGetExifInt(JpegDirectory.TagImageHeight);
            if ( width == 0 || height == 0 )
            {
                var subIfdDirectory = metadata.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                width = jpegDirectory.SafeGetExifInt(ExifDirectoryBase.TagExifImageWidth);
                height = jpegDirectory.SafeGetExifInt(ExifDirectoryBase.TagExifImageHeight);
            }
        }
    }

    #endregion
}