using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.ML;
using Damselfly.ML.FaceONNX;
using Damselfly.ML.ImageClassification;
using Damselfly.ML.ObjectDetection;
using Damselfly.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.PixelFormats;
using Image = Damselfly.Core.Models.Image;

namespace Damselfly.Core.Services;

public class ImageRecognitionService(IServiceScopeFactory _scopeFactory,
    IStatusService _statusService, ObjectDetector _objectDetector,
    MetaDataService _metdataService, FaceONNXService _faceOnnxService,
    ThumbnailService _thumbService, ConfigService _configService,
    ImageClassifier _imageClassifier, ImageCache _imageCache,
    WorkService _workService, ExifService _exifService,
    ILogger<ImageRecognitionService> _logger) : IPeopleService, IProcessJobFactory, IRescanProvider
{
    // WASM: This should be a MemoryCache
    private readonly IDictionary<string, Person> _peopleCache = new ConcurrentDictionary<string, Person>();
    
    public static bool EnableImageRecognition { get; set; } = true;

    public async Task<List<Person>> GetAllPeople()
    {
        await LoadPersonCache();

        return _peopleCache.Values.OrderBy(x => x?.Name).ToList();
    }

    public async Task<Person> GetPerson( int personId )
    {
        await LoadPersonCache();

        return _peopleCache.Values.FirstOrDefault(x => x.PersonId == personId);
    }

    public async Task<List<string>> GetPeopleNames(string searchText)
    {
        await LoadPersonCache();

        // Union the search term with the results from the DB. Order them so that the
        // closest matches to the start of the string come first.
        var names = _peopleCache.Values
            .Where(x => x.Name.StartsWith(searchText.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Name)
            .Distinct()
            .OrderBy(x => x.ToUpper().IndexOf(searchText.ToUpper()))
            .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return names;
    }

    private async Task MergeWithName(ImageContext db, int personId, string name)
    {
        var transaction = db.Database.BeginTransaction();

        try
        {
            var matchingPeople = await GetAllPeople();
            
            var newPersonId = matchingPeople.Where( x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                                            .Select( x => x.PersonId )
                                            .SingleOrDefault(-1);

            if( newPersonId != -1 )
            {
                // Update personID in image objects to the new person ID
                await db.ImageObjects.Where( x => x.PersonId == personId )
                    .ExecuteUpdateAsync( x => x.SetProperty( p => p.PersonId, newPersonId));
                
                // Update personID in FaceData
                await db.FaceData.Where( x => x.PersonId == personId )
                    .ExecuteUpdateAsync( x => x.SetProperty( p => p.PersonId, newPersonId));

                // Delete old personID
                await db.People.Where( x => x.PersonId == personId )
                    .ExecuteDeleteAsync();
                
                await transaction.CommitAsync();
            }
        }
        catch( Exception ex )
        {
            _logger.LogError( $"Exception while merging person {personId} => {name}");
            await transaction.RollbackAsync();
        }
    }

    public async Task UpdatePersonName(NameChangeRequest req)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        ImageObject imageObject = null;
        if( req.ImageObjectId != null )
            imageObject = db.ImageObjects.FirstOrDefault( x => x.ImageObjectId == req.ImageObjectId);
        
        if( req.PersonId == null )
        {
            // It's a new person. Create it and associate it with the image object
            imageObject.Person = new Person
            {
                Name = req.NewName,
                State = Person.PersonState.Identified,
                LastUpdated = DateTime.UtcNow
            };
            
            db.ImageObjects.Update(imageObject);
            await db.SaveChangesAsync("SetName");
            
        }
        else if( req.Merge )
        {
            // Merge two people together
            await MergeWithName(db, req.PersonId.Value, req.NewName);
        }
        else
        { 
            // Update the person with the new details
            await db.People.Where( x => x.PersonId == req.PersonId)
                    .ExecuteUpdateAsync( setter => setter
                    .SetProperty(p => p.Name, v => req.NewName)
                    .SetProperty(p => p.State, v => Person.PersonState.Identified)
                    .SetProperty(p => p.LastUpdated, v => DateTime.UtcNow));
        }

        // Add/update the cache and embeddings
        await LoadPersonCache(true);
        
        _imageCache.Evict(imageObject.ImageId);
        
        // TODO - if we've changed a person's name, we should find all of the images that reference that 
        // person, and evict them from the cache. But this could be exceptionally memory intensive.
    }

    public async Task UpdatePerson(Person person, string name)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        person.Name = name;
        person.State = Person.PersonState.Identified;
        person.LastUpdated = DateTime.UtcNow;
        db.People.Update(person);

        await db.SaveChangesAsync("SetName");

        // Add/update the cache
        _peopleCache[person.PersonGuid] = person;
    }

    public JobPriorities Priority => JobPriorities.ImageRecognition;

    public async Task<ICollection<IProcessJob>> GetPendingJobs(int maxJobs)
    {
        var enableAIProcessing = _configService.GetBool(ConfigSettings.EnableAIProcessing, true);

        if( enableAIProcessing )
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetService<ImageContext>();

            // Only pull out images where the thumb *has* been processed, and the
            // metadata has already been scanned, the AI hasn't been processed.
            var images = await db.ImageMetaData.Where(x => x.LastUpdated >= x.Image.LastUpdated
                                                           && x.ThumbLastUpdated != null
                                                           && x.AILastUpdated == null)
                .OrderByDescending(x => x.LastUpdated)
                .Take(maxJobs)
                .Select(x => x.ImageId)
                .ToListAsync();

            if ( images.Any() )
            {
                var jobs = images.Select(x => new AIProcess { ImageId = x, Service = this })
                    .ToArray();
                return jobs;
            }
        }

        return new AIProcess[0];
    }

    public async Task MarkFolderForScan(int folderId)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        //var queryable = db.Set<ImageMetaData>().Where(img => img.Image.FolderId == folder.FolderId);
        //int updated = await db.BatchUpdate(queryable, x => new ImageMetaData { AILastUpdated = null });

        var updated = await ImageContext.UpdateMetadataFields(db, folderId, "AILastUpdated", "null");

        if ( updated != 0 )
            _statusService.UpdateStatus($"{updated} images in folder flagged for AI reprocessing.");

        _workService.FlagNewJobs(this);
    }

    public async Task MarkAllForScan()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var updated = await db.BatchUpdate(db.ImageMetaData, i => i.SetProperty(x => x.AILastUpdated, x => null));

        _statusService.UpdateStatus($"All {updated} images flagged for AI reprocessing.");

        _workService.FlagNewJobs(this);
    }

    public async Task MarkImagesForScan(ICollection<int> images)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var queryable = db.ImageMetaData.Where(i => images.Contains(i.ImageId));

        var rows = await db.BatchUpdate(queryable, i => i.SetProperty(x => x.AILastUpdated, x => null));

        var msgText = rows == 1 ? "Image" : $"{rows} images";
        _statusService.UpdateStatus($"{msgText} flagged for AI reprocessing.");
    }

    private int GetPersonIDFromCache(Guid? PersonGuid)
    {
        if ( PersonGuid.HasValue )
        {
            // TODO Await
            LoadPersonCache().Wait();

            if ( _peopleCache.TryGetValue(PersonGuid.ToString(), out var person) )
                return person.PersonId;
        }

        return 0;
    }

    /// <summary>
    ///     Initialise the in-memory cache of people.
    /// </summary>
    /// <param name="force"></param>
    private async Task LoadPersonCache(bool force = false)
    {
        try
        {
            if ( force || !_peopleCache.Any() )
            {
                _peopleCache.Clear();

                var watch = new Stopwatch("LoadPersonCache");

                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetService<ImageContext>();
                
                var identifiedPeople = await db.People.Where(x => !string.IsNullOrEmpty(x.PersonGuid))
                    .Include(x => x.FaceData)
                    .Where( x => x.FaceData.Count > 0)
                    .AsNoTracking()
                    .ToListAsync();

                if( identifiedPeople.Any() )
                {
                    // Populate the people cache
                    foreach( var person in identifiedPeople )
                        _peopleCache[person.PersonGuid] = person;
                    
                    // Now populate the embeddings lookup
                    var embeddings = identifiedPeople.ToDictionary(
                        x => x.PersonGuid,
                        x => x.FaceData.Select( e => e.Embeddings));
                    
                    _faceOnnxService.LoadFaceEmbeddings(embeddings);

                    Logging.LogTrace("Pre-loaded cach with {0} people.", _peopleCache.Count());
                }

                watch.Stop();
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Unexpected exception loading people cache: {ex.Message}");
        }
    }

    /// <summary>
    ///     Create the DB entries for people who we don't know about,
    ///     and then pre-populate the cache with their entries.
    /// </summary>
    /// <param name="detectedFaces"></param>
    /// <returns></returns>
    public async Task CreateMissingPeople(IEnumerable<ImageDetectResult> detectedFaces)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        try
        {
            if ( detectedFaces != null )
            {
                var newFaces = detectedFaces.Where( x => x.IsNewPerson ).ToList();

                if ( newFaces.Any() )
                {
                    Logging.Log($"Adding {newFaces.Count()} person records.");

                    var newPeople = newFaces.Select(x => new Person
                    {
                        Name = "Unknown",
                        State = Person.PersonState.Unknown,
                        LastUpdated = DateTime.UtcNow, 
                        PersonGuid = x.PersonGuid,
                        FaceData = new List<PersonFaceData> { new() { Embeddings = string.Join( ",", x.Embeddings) } },
                    }).ToList();

                    if ( newPeople.Any() )
                    {
                        await db.People.AddRangeAsync( newPeople );
                        await db.SaveChangesAsync();

                        // Add or replace the new people in the cache (this should always add)
                        newPeople.ForEach(x => _peopleCache[x.PersonGuid] = x);
                    }
                }
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception in CreateMissingPeople: {ex.Message}");
        }
    }

    /// <summary>
    ///     Given a collection of detected objects, create the tags, put them in the cache,
    ///     and then return a list of keyword => TagID key-value pairs
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    private async Task<IDictionary<string, int>> CreateNewTags(IList<ImageDetectResult> objects)
    {
        var allLabels = objects.Select(x => x.Tag).Distinct().ToList();
        var tags = await _metdataService.CreateTagsFromStrings(allLabels);

        return tags.ToDictionary(x => x.Keyword, y => y.TagId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Detect objects in the image.
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    private async Task DetectObjects(ImageMetaData metadata)
    {
        var image = metadata.Image;
        var fileName = new FileInfo(image.FullPath);

        if ( !fileName.Exists )
            return;

        try
        {
            var thumbSize = ThumbSize.Large;
            var medThumb = new FileInfo(_thumbService.GetThumbPath(fileName, thumbSize));

            // We need a large thumbnail to do AI processing. Ensure it's been created.
            if( ! medThumb.Exists )
            {
                await _thumbService.CreateThumb(image.ImageId, thumbSize);
                if( ! File.Exists( medThumb.FullName ) )
                {
                    // If we couldn't create the thumb, bail out.
                    throw new InvalidOperationException(
                        $"Unable to run AI processing - {thumbSize} thumbnail doesn't exist: {medThumb}");
                }
            }

            var enableAIProcessing = _configService.GetBool(ConfigSettings.EnableAIProcessing, true);
    
            MetaDataService.GetImageSize(medThumb.FullName, out var thumbWidth, out var thumbHeight);

            var foundObjects = new List<ImageObject>();
            var foundFaces = new List<ImageObject>();

            if ( enableAIProcessing )
                Logging.Log($"Processing AI image detection for {fileName.Name}...");

            if ( !File.Exists(medThumb.FullName) )
                // The thumb isn't ready yet. 
                return;

            using var theImage = SixLabors.ImageSharp.Image.Load<Rgb24>(medThumb.FullName);

            if ( _imageClassifier != null && enableAIProcessing )
            {
                var colorWatch = new Stopwatch("DetectDominantColours");

                var dominant = _imageClassifier.DetectDominantColour(theImage);
                var average = _imageClassifier.DetectAverageColor(theImage);

                colorWatch.Stop();

                image.MetaData.AverageColor = average.ToHex();
                image.MetaData.DominantColor = dominant.ToHex();

                Logging.LogVerbose(
                    $"Image {image.FullPath} has dominant colour {dominant.ToHex()}, average {average.ToHex()}");
            }

            if( enableAIProcessing && _faceOnnxService != null)
            {
                var facewatch = new Stopwatch("FaceONNXDetect");

                var faces = await _faceOnnxService.DetectFaces(theImage);

                facewatch.Stop();

                if( faces.Any() )
                {
                    Logging.Log($" FaceONNX found {faces.Count()} faces in {fileName}...");

                    var newTags = await CreateNewTags(faces);

                    // Create any new ones, or pull existing ones back from the cache
                    await CreateMissingPeople(faces);

                    var newFaces = faces.Select(x => new ImageObject
                    {
                        RecogntionSource = ImageObject.RecognitionType.FaceONNX,
                        ImageId = image.ImageId,
                        PersonId = _peopleCache[x.PersonGuid].PersonId,
                        RectX = x.Rect.Left,
                        RectY = x.Rect.Top,
                        RectHeight = x.Rect.Height,
                        RectWidth = x.Rect.Width,
                        TagId = newTags[x.Tag],
                        Type = x.IsFace
                            ? ImageObject.ObjectTypes.Face.ToString()
                            : ImageObject.ObjectTypes.Object.ToString(),
                        Score = 0
                    }).ToList();

                    ScaleObjectRects(image, newFaces, thumbWidth, thumbHeight);
                    foundFaces.AddRange(newFaces);

                }
            }
            
            // For the object detector, we need a successfully loaded bitmap
            if ( enableAIProcessing )
            {
                var objwatch = new Stopwatch("DetectObjects");

                // First, look for Objects
                var objects = await _objectDetector.DetectObjects(theImage);

                objwatch.Stop();

                if ( objects.Any() )
                {
                    Logging.Log($" Yolo found {objects.Count()} objects in {fileName}...");

                    var newTags = await CreateNewTags(objects);

                    var newObjects = objects.Select(x => new ImageObject
                    {
                        RecogntionSource = ImageObject.RecognitionType.MLNetObject,
                        ImageId = image.ImageId,
                        RectX = x.Rect.Left,
                        RectY = x.Rect.Top,
                        RectHeight = x.Rect.Height,
                        RectWidth = x.Rect.Width,
                        TagId = x.IsFace ? 0 : newTags[x.Tag],
                        Type = ImageObject.ObjectTypes.Object.ToString(),
                        Score = x.Score
                    }).ToList();
                    
                    ScaleObjectRects(image, newObjects, thumbWidth, thumbHeight);
                    foundObjects.AddRange(newObjects);
                }
            }

            if ( foundFaces.Any() )
            {
                // We've found some faces. Add a tagID.
                const string faceTagName = "Face";
                var tags = await _metdataService.CreateTagsFromStrings(new List<string> { faceTagName });
                var faceTagId = tags.Single().TagId;
                foundFaces.ForEach(x => x.TagId = faceTagId);
            }

            if ( foundObjects.Any() || foundFaces.Any() )
            {
                var objWriteWatch = new Stopwatch("WriteDetectedObjects");

                var allFound = foundObjects.Union(foundFaces).ToList();

                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetService<ImageContext>();

                // First, clear out the existing faces and objects - we don't want dupes
                // TODO: Might need to be smarter about this once we add face names and
                // Object identification details.
                await db.BatchDelete(db.ImageObjects.Where(x =>
                    x.ImageId.Equals(image.ImageId) && x.RecogntionSource != ImageObject.RecognitionType.ExternalApp));
                // Now add the objects and faces.
                await db.BulkInsert(db.ImageObjects, allFound);

                WriteAITagsToImages(image, allFound);

                objWriteWatch.Stop();
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception during AI detection for {fileName}: {ex}");
        }
    }

    /// <summary>
    ///     Write the tags to the image
    /// </summary>
    /// <param name="tags"></param>
    private void WriteAITagsToImages(Image image, List<ImageObject> tags)
    {
        if ( _configService.GetBool(ConfigSettings.WriteAITagsToImages) )
        {
            Logging.Log("Writing AI tags to image Metadata...");

            // Seleect the tag IDs that aren't faces.
            var tagIdsToAdd = tags.Where(x => !x.IsFace)
                .Select(x => x.TagId)
                .Distinct()
                .ToList();

            if ( tagIdsToAdd.Any() )
            {
                // Get their keywords
                var keywordsToAdd = _metdataService.CachedTags
                    .Where(x => tagIdsToAdd.Contains(x.TagId))
                    .Select(x => x.Keyword)
                    .ToList();

                if ( keywordsToAdd.Any() )
                    // Fire and forget this asynchronously - we don't care about waiting for it
                    _ = _exifService.UpdateTagsAsync(image, keywordsToAdd);
            }

            // Seleect the tag IDs that are faces.
            var facesToAdd = tags.Where(x => x.IsFace)
                .Distinct()
                .ToList();

            if ( facesToAdd.Any() )
                // Fire and forget this asynchronously - we don't care about waiting for it
                _ = _exifService.UpdateFaceDataAsync(new[] { image }, facesToAdd);
        }
    }

    /// <summary>
    ///     Scales the detected face/object rectangles based on the full-sized image,
    ///     since the object detection was done on a smaller thumbnail.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="imgObjects">Collection of objects to scale</param>
    /// <param name="thumbSize"></param>
    private void ScaleObjectRects(Image image, List<ImageObject> imgObjects, int bmpWidth, int bmpHeight)
    {
        if ( bmpHeight == 0 || bmpWidth == 0 )
            return;

        float longestBmpSide = bmpWidth > bmpHeight ? bmpWidth : bmpHeight;
        float longestImgSide =
            image.MetaData.Width > image.MetaData.Height ? image.MetaData.Width : image.MetaData.Height;
        var ratio = longestImgSide / longestBmpSide;

        foreach ( var imgObj in imgObjects )
        {
            imgObj.RectX = (int)(imgObj.RectX * ratio);
            imgObj.RectY = (int)(imgObj.RectY * ratio);
            imgObj.RectWidth = (int)(imgObj.RectWidth * ratio);
            imgObj.RectHeight = (int)(imgObj.RectHeight * ratio);
        }
    }

    public void StartService()
    {
        if ( !EnableImageRecognition )
        {
            Logging.Log("AI Image recognition service was disabled.");
            return;
        }

        _ = LoadPersonCache();

        _workService.AddJobSource(this);
    }

    /// <summary>
    ///     Work processing method for AI processing for a single
    ///     Image.
    /// </summary>
    /// <param name="imageId"></param>
    /// <returns></returns>
    private async Task DetectObjects(int imageId)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var image = await _imageCache.GetCachedImage(imageId);

        db.Attach(image);

        // First, update the timestamp. We do this first, so that even if something
        // fails, it'll be set, avoiding infinite loops of failed processing.
        // The caller will update the DB with a SaveChanges call.
        image.MetaData.AILastUpdated = DateTime.UtcNow;

        try
        {
            await DetectObjects(image.MetaData);
        }
        finally
        {
            // The DetectObjects method will set the metadata AILastUpdated
            // timestamp. It may also update other fields.
            db.ImageMetaData.Update(image.MetaData);
            await db.SaveChangesAsync("UpdateAIGenDate");
        }

        _imageCache.Evict(imageId);
    }

    public class AIProcess : IProcessJob
    {
        public int ImageId { get; set; }
        public ImageRecognitionService Service { get; set; }
        public string Name => "AI processing";
        public string Description => $"{Name} for ID: {ImageId}";
        public JobPriorities Priority => JobPriorities.ImageRecognition;

        public async Task Process()
        {
            await Service.DetectObjects(ImageId);
        }

        public bool CanProcess => true;

        public override string ToString()
        {
            return Description;
        }
    }
}
