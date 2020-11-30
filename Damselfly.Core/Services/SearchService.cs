using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;

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
        public SearchService()
        {
            Instance = this;
        }

        private readonly SearchQuery query = new SearchQuery();
        public List<Image> SearchResults { get; private set; } = new List<Image>();

        public void NotifyStateChanged()
        {
            Logging.LogVerbose($"Filter changed: {query}");

            OnChange?.Invoke();
        }

        public event Action OnChange;

        public static SearchService Instance { get; private set; }

        public string SearchText { get { return query.SearchText; } set { if (query.SearchText != value.Trim() ) { query.SearchText = value.Trim(); QueryChanged(); } } }
        public DateTime MaxDate { get { return query.MaxDate; } set { if (query.MaxDate != value) { query.MaxDate = value; QueryChanged(); } } }
        public DateTime MinDate { get { return query.MinDate; } set { if (query.MinDate != value) { query.MinDate = value; QueryChanged(); } } }
        public ulong MaxSizeKB { get { return query.MaxSizeKB; } set { if (query.MaxSizeKB != value) { query.MaxSizeKB = value; QueryChanged(); } } }
        public ulong MinSizeKB { get { return query.MinSizeKB; } set { if (query.MinSizeKB != value) { query.MinSizeKB = value; QueryChanged(); } } }
        public Folder Folder { get { return query.Folder; } set { if (query.Folder != value) { query.Folder = value; QueryChanged(); } } }
        public bool TagsOnly { get { return query.TagsOnly; } set { if (query.TagsOnly != value) { query.TagsOnly = value; QueryChanged(); } } }

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
                Image[] results = new Image[0];

                try
                {
                    Logging.LogTrace("Loading images from {0} to {1} - Query: {2}", first, first + count, query);

                    bool hasTextSearch = !string.IsNullOrEmpty(query.SearchText);

                    // Default is everything.
                    IQueryable<Image> images = db.Images.AsQueryable();

                    if (hasTextSearch)
                    {
                        // If we have search text, then hit the fulltext Search.
                        images = db.ImageSearch(query.SearchText);
                    }

                    images = images.Include(x => x.Folder);

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

                    images = images.Include(x => x.MetaData);

                    if (query.MinDate > DateTime.MinValue || query.MaxDate < DateTime.MaxValue)
                    {
                        // Always filter by date - because if there's no filter
                        // set then they'll be set to min/max date.
                        images = images.Where(x => x.MetaData == null ||
                                                  (x.MetaData.DateTaken >= query.MinDate &&
                                                   x.MetaData.DateTaken <= query.MaxDate));
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

                    images = images.Include(x => x.BasketEntry);
                    // Disable this for now - it's slow due to the EFCore subquery bug
                    // images = images.Include(x => x.ImageTags)
                    //               .ThenInclude(x => x.Tag);

                    results = await images.OrderByDescending(x => x.MetaData.DateTaken)
                                    .Skip(first)
                                    .Take(count)
                                    .ToArrayAsync();
                }
                catch (Exception ex)
                {
                    Logging.LogError("Search query failed: {0}", ex.Message);
                }
                finally
                {
                    watch.Stop();
                }

                Logging.Log($"Search: {results.Count()} images found in search query within {watch.ElapsedTime}ms.");
                StatusService.Instance.StatusText = $"Found at least {results.Count()} images that match the search query.";

                // Now save the results in our stored dataset
                SearchResults.AddRange(results);
            }
        }

        /// <summary>
        /// Load some initial data into memory when we first start up.
        /// </summary>
        public void PreLoadSearchData()
        {
            _ = LoadMoreData(0, 100);
        }

        public async Task<Image[]> GetQueryImagesAsync(int first, int count)
        {
            // Load more data if we need it.
            await LoadMoreData(first, count);

            return SearchResults.Skip(first).Take(count).ToArray();
        }
    }
}
