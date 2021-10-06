using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.Services;
using Humanizer;
using static Damselfly.Core.Models.SearchQuery;

namespace Damselfly.Core.ScopedServices
{
    /// <summary>
    /// The search service manages the current set of parameters that make up the search
    /// query, and thus determine the set of results returned to the image browser list.
    /// The results are stored here, and returned as a virtualised set - so we only pass
    /// back (say) 200 images, and then requery for the next 200 when the user scrolls.
    /// This saves us returning thousands of items for a search.
    /// </summary>
    public class SearchService
    {
        public SearchService( UserStatusService statusService, ImageCache cache, IndexingService indexingService)
        {
            _statusService = statusService;
            _imageCache = cache;
            _indexingService = indexingService;
        }

        private readonly UserStatusService _statusService;
        private readonly ImageCache _imageCache;
        private readonly IndexingService _indexingService;
        private readonly SearchQuery query = new SearchQuery();
        public List<Image> SearchResults { get; private set; } = new List<Image>();
        private const double s_similarityThreshold = 0.75;

        public void NotifyStateChanged()
        {
            Logging.LogVerbose($"Filter changed: {query}");

            OnChange?.Invoke();
        }

        public event Action OnChange;

        public string SearchText { get { return query.SearchText; } set { if (query.SearchText != value.Trim() ) { query.SearchText = value.Trim(); QueryChanged(); } } }
        public DateTime? MaxDate { get { return query.MaxDate; } set { if (query.MaxDate != value) { query.MaxDate = value; QueryChanged(); } } }
        public DateTime? MinDate { get { return query.MinDate; } set { if (query.MinDate != value) { query.MinDate = value; QueryChanged(); } } }
        public int? MaxSizeKB { get { return query.MaxSizeKB; } set { if (query.MaxSizeKB != value) { query.MaxSizeKB = value; QueryChanged(); } } }
        public int? MinSizeKB { get { return query.MinSizeKB; } set { if (query.MinSizeKB != value) { query.MinSizeKB = value; QueryChanged(); } } }
        public Folder Folder { get { return query.Folder; } set { if (query.Folder != value) { query.Folder = value; QueryChanged(); } } }
        public bool TagsOnly { get { return query.TagsOnly; } set { if (query.TagsOnly != value) { query.TagsOnly = value; QueryChanged(); } } }
        public bool IncludeAITags { get { return query.IncludeAITags; } set { if (query.IncludeAITags != value) { query.IncludeAITags = value; QueryChanged(); } } }
        public bool UntaggedImages { get { return query.UntaggedImages; } set { if (query.UntaggedImages != value) { query.UntaggedImages = value; QueryChanged(); } } }
        public int CameraId { get { return query.CameraId; } set { if (query.CameraId != value) { query.CameraId = value; QueryChanged(); } } }
        public Tag Tag { get { return query.Tag; } set { if (query.Tag != value) { query.Tag = value; QueryChanged(); } } }
        public int LensId { get { return query.LensId; } set { if (query.LensId != value) { query.LensId = value; QueryChanged(); } } }
        public Image SimilarTo { get { return query.SimilarTo; } set { if (query.SimilarTo != value) { query.SimilarTo = value; QueryChanged(); } } }
        public GroupingType Grouping { get { return query.Grouping; } set { if (query.Grouping != value) { query.Grouping = value; QueryChanged(); } } }
        public SortOrderType SortOrder { get { return query.SortOrder; } set { if (query.SortOrder != value) { query.SortOrder = value; QueryChanged(); } } }
        public FaceSearchType FaceSearch { get { return query.FaceSearch; } set { if (query.FaceSearch != value) { query.FaceSearch = value; QueryChanged(); } } }
        public OrientationType Orientation { get { return query.Orientation; } set { if (query.Orientation != value) { query.Orientation = value; QueryChanged(); } } }

        public void Reset() { ApplyQuery(new SearchQuery()); }
        public void Refresh() { QueryChanged(); }

        public void ApplyQuery(SearchQuery newQuery)
        {
            if (newQuery.CopyPropertiesTo(query))
            {
                QueryChanged();
            }
        }

        public void SetDateRange( DateTime? min, DateTime? max )
        {
            if (query.MinDate != min || query.MaxDate != max)
            {
                query.MinDate = min;
                query.MaxDate = max;
                QueryChanged();
            }
        }

        private void QueryChanged()
        {
            SearchResults.Clear();
            NotifyStateChanged();
        }

        /// <summary>
        /// Escape out characters like apostrophes
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns></returns>
        private static string EscapeChars( string searchText )
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
        private async Task LoadMoreData(int first, int count)
        {
            if (first < SearchResults.Count() && first + count < SearchResults.Count())
            {
                // Data already loaded. Nothing to do.
                return;
            }

            if (SearchResults.Count > first)
            {
                int firstOffset = SearchResults.Count - first;
                first = SearchResults.Count;
                count -= firstOffset;
            }

            if (count > 0)
            {
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
                        var searchText = EscapeChars( query.SearchText );
                        // If we have search text, then hit the fulltext Search.
                        images = await db.ImageSearch(searchText, query.IncludeAITags);
                    }

                    images = images.Include(x => x.Folder);

                    if ( query.Tag != null )
                    {
                        var tagImages = images.Where(x => x.ImageTags.Any(y => y.TagId == query.Tag.TagId));
                        var objImages = images.Where(x => x.ImageObjects.Any(y => y.TagId == query.Tag.TagId));

                        images = tagImages.Union(objImages);
                    }

                    if( query.UntaggedImages )
                    {
                        images = images.Where(x => ! x.ImageTags.Any() );
                    }

                    if( query.SimilarTo != null && query.SimilarTo.Hash != null )
                    {
                        images = images.Where(x => x.ImageId != SimilarTo.ImageId &&
                                   (x.Hash.PerceptualHex1 == SimilarTo.Hash.PerceptualHex1 ||
                                    x.Hash.PerceptualHex2 == SimilarTo.Hash.PerceptualHex2 ||
                                    x.Hash.PerceptualHex3 == SimilarTo.Hash.PerceptualHex3 ||
                                    x.Hash.PerceptualHex4 == SimilarTo.Hash.PerceptualHex4));
                    }

                    // If selected, filter by the image filename/foldername
                    if (hasTextSearch && ! query.TagsOnly )
                    {
                        // TODO: Make this like more efficient. Toggle filename/path search? Or just add filename into FTS?
                        string likeTerm = $"%{query.SearchText}%";

                        // Now, search folder/filenames
                        var fileImages = db.Images.Include(x => x.Folder)
                                                    .Where(x => EF.Functions.Like(x.Folder.Path, likeTerm)
                                                            || EF.Functions.Like(x.FileName, likeTerm));
                        images = images.Union(fileImages);
                    }

                    if (query.Folder?.FolderId >= 0)
                    {
                        // Filter by folderID
                        images = images.Where(x => x.FolderId == query.Folder.FolderId);
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

                    if( query.MinSizeKB.HasValue )
                    {
                        int minSizeBytes = query.MinSizeKB.Value * 1024;
                        images = images.Where(x => x.FileSizeBytes > minSizeBytes);
                    }

                    if (query.MaxSizeKB.HasValue )
                    {
                        int maxSizeBytes = query.MaxSizeKB.Value * 1024;
                        images = images.Where(x => x.FileSizeBytes < maxSizeBytes);
                    }

                    if( query.Orientation != OrientationType.All )
                    {
                        if (query.Orientation == OrientationType.Landscape)
                            images = images.Where(x => x.MetaData.Width > x.MetaData.Height);
                        else
                            images = images.Where(x => x.MetaData.Height > x.MetaData.Width);
                    }

                    if (query.CameraId != -1 || query.LensId != -1)
                    {
                        images = images.Include(x => x.MetaData);

                        if (query.CameraId != -1)
                            images = images.Where(x => x.MetaData.CameraId == query.CameraId);
 
                        if (query.LensId != -1)
                            images = images.Where(x => x.MetaData.LensId == query.LensId);
                    }

                    if( query.FaceSearch != FaceSearchType.None )
                    {
                        images = query.FaceSearch switch
                        {
                            FaceSearchType.Faces => images.Where(x => x.ImageObjects.Any(x => x.Type == ImageObject.ObjectTypes.Face.ToString() )),
                            FaceSearchType.NoFaces => images.Where(x => !x.ImageObjects.Any(x => x.Type == ImageObject.ObjectTypes.Face.ToString() )),
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

                    results = await images.Select( x => x.ImageId )
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

                // Now load the tags....
                var enrichedImages = await _imageCache.GetCachedImages(results);

                try
                {
                    // If it's a 'similar to' query, filter out the ones that don't pass the threshold.
                    if (query.SimilarTo != null)
                    {
                        // Complete the hamming distance calculation here:
                        var searchHash = query.SimilarTo.Hash;

                        var similarImages = enrichedImages.Where(x => x.Hash != null && x.Hash.SimilarityTo(searchHash) > s_similarityThreshold).ToList();

                        Logging.Log($"Found {similarImages.Count} images that match image ID {query.SimilarTo.ImageId} with a threshold of {s_similarityThreshold:P1} or more.");

                        enrichedImages = similarImages;
                    }
                }
                catch ( Exception ex )
                {
                    Logging.LogError($"Similarity threshold calculation failed: {ex}");
                }

                // Set the results on the service property
                SearchResults.AddRange(enrichedImages);

                _statusService.StatusText = $"Found at least {enrichedImages.Count} images that match the search query.";
            }
        }

        public async Task<Image[]> GetQueryImagesAsync(int first, int count)
        {
            // Load more data if we need it.
            await LoadMoreData(first, count);

            return SearchResults.Skip(first).Take(count).ToArray();
        }

        public string SearchBreadcrumbs
        {
            get
            {
                var hints = new List<string>();

                if (!string.IsNullOrEmpty(SearchText))
                    hints.Add($"Text: {SearchText}");

                if (Folder != null)
                    hints.Add($"Folder: {Folder.Name}");

                if (SimilarTo != null)
                    hints.Add($"Looks Like: {SimilarTo.FileName}");

                if (Tag != null)
                    hints.Add($"Tag: {Tag.Keyword}");

                string dateRange = string.Empty;
                if (MinDate.HasValue)
                    dateRange = $"{MinDate:dd-MMM-yyyy}";

                if (MaxDate.HasValue &&
                   (! MinDate.HasValue || MaxDate.Value.Date != MinDate.Value.Date))
                {
                    if (!string.IsNullOrEmpty(dateRange))
                        dateRange += " - ";
                    dateRange += $"{MaxDate:dd-MMM-yyyy}";
                }

                if (!string.IsNullOrEmpty(dateRange))
                    hints.Add($"Date: {dateRange}");

                if (UntaggedImages)
                    hints.Add($"Untagged images");

                if (FaceSearch != FaceSearchType.None)
                    hints.Add($"{FaceSearch.Humanize()}");

                if (Orientation != OrientationType.All)
                    hints.Add($"{Orientation.Humanize()}");

                if ( CameraId > 0 )
                {
                    var cam = _indexingService.Cameras.FirstOrDefault(x => x.CameraId == CameraId);
                    if (cam != null)
                        hints.Add($"Camera: {cam.Model}");
                }

                if (LensId > 0)
                {
                    var lens = _indexingService.Lenses.FirstOrDefault(x => x.LensId == LensId);
                    if (lens != null)
                        hints.Add($"Lens: {lens.Model}");
                }

                if( hints.Any() )
                    return string.Join(", ", hints);

                return "No Filter";
            }
        }
    }
}
