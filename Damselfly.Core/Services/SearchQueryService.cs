using System;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Damselfly.Core.Constants;
using static Damselfly.Core.Models.SearchQuery;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.Services;

public class SearchQueryService
{
    private readonly IStatusService _statusService;
    private readonly ImageCache _imageCache;
    private readonly IConfigService _configService;
    private readonly MetaDataService _metadataService;

    public SearchQueryService(IStatusService statusService, ImageCache cache,
                            MetaDataService metadataService, IConfigService configService)
    {
        _configService = configService;
        _statusService = statusService;
        _imageCache = cache;
        _metadataService = metadataService;
    }

    /// <summary>
    /// Escape out characters like apostrophes
    /// </summary>
    /// <param name="searchText"></param>
    /// <returns></returns>
    private static string EscapeChars(string searchText)
    {
        return searchText.Replace("'", "''");
    }

    /// <summary>
    /// The actual search query. Given a page (first+count) we run the search query on the DB
    /// and return back a set of data into the SearchResults collection. Since search parameters
    /// are all AND based, and additive, we build up the query depending on whether the user
    /// has specified a folder, a search text, a date range, etc, etc.
    /// TODO: Add support for searching by Lens ID, Camera ID, etc.
    /// </summary>
    /// <param name="first"></param>
    /// <param name="count"></param>
    /// <returns>True if there's more data available for the requested range</returns>
    private async Task<SearchResponse> LoadMoreData(SearchQuery query, int first, int count)
    {
        // Assume there is more data available - unless the search
        // returns less than we asked for (see below).
        var response = new SearchResponse { MoreDataAvailable = true, SearchResults = new Image[0] };

        using var db = new ImageContext();
        var watch = new Stopwatch("ImagesLoadData");
        List<int> results = new List<int>();

        try
        {
            Logging.LogTrace("Loading images from {0} to {1} - Query: {2}", first, first + count, query);

            bool hasTextSearch = !string.IsNullOrEmpty(query.SearchText);

            // Default is everything.
            IQueryable<Image> images = db.Images.AsQueryable();

            if (hasTextSearch)
            {
                var searchText = EscapeChars(query.SearchText);
                // If we have search text, then hit the fulltext Search.
                images = await db.ImageSearch(searchText, query.IncludeAITags);
            }

            if (query.Tag != null)
            {
                var tagImages = images.Where(x => x.ImageTags.Any(y => y.TagId == query.Tag.TagId));
                var objImages = images.Where(x => x.ImageObjects.Any(y => y.TagId == query.Tag.TagId));

                images = tagImages.Union(objImages);
            }

            if (query.UntaggedImages)
            {
                images = images.Where(x => !x.ImageTags.Any());
            }

            if (query.SimilarTo != null && query.SimilarTo.Hash != null)
            {
                var hash1A = $"{query.SimilarTo.Hash.PerceptualHex1.Substring(0, 2)}%";
                var hash1B = $"%{query.SimilarTo.Hash.PerceptualHex1.Substring(2, 2)}";
                var hash2A = $"{query.SimilarTo.Hash.PerceptualHex2.Substring(0, 2)}%";
                var hash2B = $"%{query.SimilarTo.Hash.PerceptualHex2.Substring(2, 2)}";
                var hash3A = $"{query.SimilarTo.Hash.PerceptualHex3.Substring(0, 2)}%";
                var hash3B = $"%{query.SimilarTo.Hash.PerceptualHex3.Substring(2, 2)}";
                var hash4A = $"{query.SimilarTo.Hash.PerceptualHex4.Substring(0, 2)}%";
                var hash4B = $"%{query.SimilarTo.Hash.PerceptualHex4.Substring(2, 2)}";

                images = images.Where(x => x.ImageId != query.SimilarTo.ImageId &&
                           (
                            EF.Functions.Like(x.Hash.PerceptualHex1, hash1A) ||
                            EF.Functions.Like(x.Hash.PerceptualHex1, hash1B) ||
                            EF.Functions.Like(x.Hash.PerceptualHex2, hash2A) ||
                            EF.Functions.Like(x.Hash.PerceptualHex2, hash2B) ||
                            EF.Functions.Like(x.Hash.PerceptualHex3, hash3A) ||
                            EF.Functions.Like(x.Hash.PerceptualHex3, hash3B) ||
                            EF.Functions.Like(x.Hash.PerceptualHex4, hash4A) ||
                            EF.Functions.Like(x.Hash.PerceptualHex4, hash4B)
                           ));
            }

            // If selected, filter by the image filename/foldername
            if (hasTextSearch && !query.TagsOnly)
            {
                // TODO: Make this like more efficient. Toggle filename/path search? Or just add filename into FTS?
                string likeTerm = $"%{query.SearchText}%";

                // Now, search folder/filenames
                var fileImages = db.Images.Where(x => EF.Functions.Like(x.Folder.Path, likeTerm)
                                                    || EF.Functions.Like(x.FileName, likeTerm));
                images = images.Union(fileImages);
            }

            if (query.Person?.PersonId >= 0)
            {
                // Filter by personID
                images = images.Where(x => x.ImageObjects.Any(p => p.PersonId == query.Person.PersonId));
            }

            if (query.Folder?.FolderId >= 0)
            {
                var descendants = query.Folder.Subfolders.ToList();

                // Filter by folderID
                images = images.Where(x => descendants.Select(x => x.FolderId).Contains(x.FolderId));
            }

            if (query.MinDate.HasValue || query.MaxDate.HasValue)
            {
                var minDate = query.MinDate.HasValue ? query.MinDate : DateTime.MinValue;
                var maxDate = query.MaxDate.HasValue ? query.MaxDate : DateTime.MaxValue;
                // Always filter by date - because if there's no filter
                // set then they'll be set to min/max date.
                images = images.Where(x => x.SortDate >= minDate &&
                                           x.SortDate <= maxDate);
            }

            if (query.MinRating.HasValue)
            {
                // Filter by Minimum rating
                images = images.Where(x => x.MetaData.Rating >= query.MinRating);
            }

            if (query.Month.HasValue)
            {
                // Filter by month
                images = images.Where(x => x.SortDate.Month == query.Month);
            }

            if (query.MinSizeKB.HasValue)
            {
                int minSizeBytes = query.MinSizeKB.Value * 1024;
                images = images.Where(x => x.FileSizeBytes > minSizeBytes);
            }

            if (query.MaxSizeKB.HasValue)
            {
                int maxSizeBytes = query.MaxSizeKB.Value * 1024;
                images = images.Where(x => x.FileSizeBytes < maxSizeBytes);
            }

            if (query.Orientation.HasValue)
            {
                if (query.Orientation == OrientationType.Panorama)
                    images = images.Where(x => x.MetaData.AspectRatio > 2);
                else if (query.Orientation == OrientationType.Landscape)
                    images = images.Where(x => x.MetaData.AspectRatio > 1);
                else if (query.Orientation == OrientationType.Portrait)
                    images = images.Where(x => x.MetaData.AspectRatio < 1);
                else if (query.Orientation == OrientationType.Square)
                    images = images.Where(x => x.MetaData.AspectRatio == 1);
            }

            if (query.CameraId.HasValue)
                images = images.Where(x => x.MetaData.CameraId == query.CameraId);

            if (query.LensId.HasValue)
                images = images.Where(x => x.MetaData.LensId == query.LensId);

            if (query.FaceSearch.HasValue)
            {
                images = query.FaceSearch switch
                {
                    FaceSearchType.Faces => images.Where(x => x.ImageObjects.Any(x => x.Type == ImageObject.ObjectTypes.Face.ToString())),
                    FaceSearchType.NoFaces => images.Where(x => !x.ImageObjects.Any(x => x.Type == ImageObject.ObjectTypes.Face.ToString())),
                    FaceSearchType.UnidentifiedFaces => images.Where(x => x.ImageObjects.Any(x => x.Person.State == Person.PersonState.Unknown)),
                    FaceSearchType.IdentifiedFaces => images.Where(x => x.ImageObjects.Any(x => x.Person.State == Person.PersonState.Identified)),
                    _ => images
                };

            }

            // Add in the ordering for the group by
            switch (query.Grouping)
            {
                case GroupingType.None:
                case GroupingType.Date:
                    images = query.SortOrder == SortOrderType.Descending ?
                                   images.OrderByDescending(x => x.SortDate) :
                                   images.OrderBy(x => x.SortDate);
                    break;
                case GroupingType.Folder:
                    images = query.SortOrder == SortOrderType.Descending ?
                                   images.OrderBy(x => x.Folder.Path).ThenByDescending(x => x.SortDate) :
                                   images.OrderByDescending(x => x.Folder.Path).ThenBy(x => x.SortDate);
                    break;
                default:
                    throw new ArgumentException("Unexpected grouping type.");
            }

            results = await images.Select(x => x.ImageId)
                            .Skip(first)
                            .Take(count)
                            .ToListAsync();

            watch.Stop();

            Logging.Log($"Search: {results.Count()} images found in search query within {watch.ElapsedTime}ms");
        }
        catch (Exception ex)
        {
            Logging.LogError("Search query failed: {0}", ex.Message);
        }
        finally
        {
            watch.Stop();
        }

        if (results.Count < count)
        {
            // The number of returned IDs is less than we asked for
            // so we must have reached the end of the results.
            response.MoreDataAvailable = false;
        }

        // Now load the tags....
        var enrichedImages = await _imageCache.GetCachedImages(results);

        try
        {
            // If it's a 'similar to' query, filter out the ones that don't pass the threshold.
            if (query.SimilarTo != null && enrichedImages.Any())
            {
                double threshold = _configService.GetInt(ConfigSettings.SimilarityThreshold, 75) / 100.0;

                // Complete the hamming distance calculation here:
                var searchHash = query.SimilarTo.Hash;

                var similarImages = enrichedImages.Where(x => x.Hash != null && x.Hash.SimilarityTo(searchHash) > threshold).ToList();

                Logging.Log($"Found {similarImages.Count} of {enrichedImages.Count} prefiltered images that match image ID {query.SimilarTo.ImageId} with a threshold of {threshold:P1} or more.");

                enrichedImages = similarImages;
            }
        }
        catch (Exception ex)
        {
            Logging.LogError($"Similarity threshold calculation failed: {ex}");
        }

        // Set the results on the service property
        response.SearchResults = enrichedImages.ToArray();

        _statusService.StatusText = $"Found at least {enrichedImages.Count} images that match the search query.";

        return response;
    }

    public async Task<SearchResponse> GetQueryImagesAsync(SearchRequest request)
    {
        // Load more data if we need it.
        return await LoadMoreData(request.Query, request.First, request.Count);
   }
}

