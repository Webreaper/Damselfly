using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;
using static Damselfly.Core.Models.SearchQuery;

namespace Damselfly.Core.Services
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
        public SearchService( UserStatusService statusService )
        {
            _statusService = statusService;
        }

        private readonly UserStatusService _statusService;
        private readonly SearchQuery query = new SearchQuery();
        public List<Image> SearchResults { get; private set; } = new List<Image>();
        private IDictionary<int, Image> imageCache = new Dictionary<int, Image>();

        public Image GetFromCache( int imageId )
        {
            if (imageCache.TryGetValue(imageId, out var Image))
                return Image;

            return null;
        }

        public void NotifyStateChanged()
        {
            Logging.LogVerbose($"Filter changed: {query}");

            OnChange?.Invoke();
        }

        public event Action OnChange;

        public string SearchText { get { return query.SearchText; } set { if (query.SearchText != value.Trim() ) { query.SearchText = value.Trim(); QueryChanged(); } } }
        public DateTime MaxDate { get { return query.MaxDate; } set { if (query.MaxDate != value) { query.MaxDate = value; QueryChanged(); } } }
        public DateTime MinDate { get { return query.MinDate; } set { if (query.MinDate != value) { query.MinDate = value; QueryChanged(); } } }
        public ulong MaxSizeKB { get { return query.MaxSizeKB; } set { if (query.MaxSizeKB != value) { query.MaxSizeKB = value; QueryChanged(); } } }
        public ulong MinSizeKB { get { return query.MinSizeKB; } set { if (query.MinSizeKB != value) { query.MinSizeKB = value; QueryChanged(); } } }
        public Folder Folder { get { return query.Folder; } set { if (query.Folder != value) { query.Folder = value; QueryChanged(); } } }
        public bool TagsOnly { get { return query.TagsOnly; } set { if (query.TagsOnly != value) { query.TagsOnly = value; QueryChanged(); } } }
        public bool IncludeAITags { get { return query.IncludeAITags; } set { if (query.IncludeAITags != value) { query.IncludeAITags = value; QueryChanged(); } } }
        public int CameraId { get { return query.CameraId; } set { if (query.CameraId != value) { query.CameraId = value; QueryChanged(); } } }
        public int TagId { get { return query.TagId; } set { if (query.TagId != value) { query.TagId = value; QueryChanged(); } } }
        public int LensId { get { return query.LensId; } set { if (query.LensId != value) { query.LensId = value; QueryChanged(); } } }
        public GroupingType Grouping { get { return query.Grouping; } set { if (query.Grouping != value) { query.Grouping = value; QueryChanged(); } } }
        public SortOrderType SortOrder { get { return query.SortOrder; } set { if (query.SortOrder != value) { query.SortOrder = value; QueryChanged(); } } }

        public void SetDateRange( DateTime min, DateTime max )
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
                Stopwatch tagwatch = null;
                Image[] results = new Image[0];

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

                    if ( query.TagId != -1 )
                    {
                        var tagImages = images.Where(x => x.ImageTags.Any(y => y.TagId == query.TagId));
                        var objImages = images.Where(x => x.ImageObjects.Any(y => y.TagId == query.TagId));

                        images = tagImages.Union(objImages);
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

                    if (query.MinDate > DateTime.MinValue || query.MaxDate < DateTime.MaxValue)
                    {
                        // Always filter by date - because if there's no filter
                        // set then they'll be set to min/max date.
                        images = images.Where(x => x.SortDate >= query.MinDate &&
                                                   x.SortDate <= query.MaxDate);
                    }

                    if( query.MinSizeKB > ulong.MinValue )
                    {
                        ulong minSizeBytes = query.MinSizeKB / 1024;
                        images = images.Where(x => x.FileSizeBytes >= minSizeBytes);
                    }

                    if (query.MaxSizeKB < ulong.MaxValue )
                    {
                        ulong maxSizeBytes = query.MaxSizeKB / 1024;
                        images = images.Where(x => x.FileSizeBytes <= maxSizeBytes);
                    }

                    images = images.Include(x => x.MetaData);
                    images = images.Include(x => x.BasketEntry);

                    if ( query.CameraId != -1 )
                    {
                        images = images.Where(x => x.MetaData.CameraId == query.CameraId);
                    }

                    if (query.LensId != -1)
                    {
                        images = images.Where(x => x.MetaData.LensId == query.LensId);
                    }

                    // Disable this for now - it's slow due to the EFCore subquery bug.
                    // We mitigate it by loading the tags in a separate query below.
                    // images = images.Include(x => x.ImageTags)
                    //               .ThenInclude(x => x.Tag);

                    results = await images.Skip(first)
                                    .Take(count)
                                    .ToArrayAsync();

                    tagwatch = new Stopwatch("SearchLoadTags");

                    // Now load the tags....
                    foreach (var img in results)
                    {
                        imageCache[img.ImageId] = img;
                        await db.LoadTags(img);
                    }

                    tagwatch.Stop();
                }
                catch (Exception ex)
                {
                    Logging.LogError("Search query failed: {0}", ex.Message);
                }
                finally
                {
                    watch.Stop();
                }

                Logging.Log($"Search: {results.Count()} images found in search query within {watch.ElapsedTime}ms (Tags: {tagwatch.ElapsedTime}ms)");
                _statusService.StatusText = $"Found at least {first + results.Count()} images that match the search query.";

                // Now save the results in our stored dataset
                SearchResults.AddRange(results);
            }
        }

        public async Task<Image[]> GetQueryImagesAsync(int first, int count)
        {
            // Load more data if we need it.
            await LoadMoreData(first, count);

            return SearchResults.Skip(first).Take(count).ToArray();
        }
    }
}
