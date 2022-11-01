using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Damselfly.Core.Utils;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

/// <summary>
///     The search service manages the current set of parameters that make up the search
///     query, and thus determine the set of results returned to the image browser list.
///     The results are stored here, and returned as a virtualised set - so we only pass
///     back (say) 200 images, and then requery for the next 200 when the user scrolls.
///     This saves us returning thousands of items for a search.
/// </summary>
public class ServerSearchService : BaseSearchService, ISearchService
{
    private readonly SearchQueryService _queryService;

    public ServerSearchService(ICachedDataService dataService, IImageCacheService imageCache, SearchQueryService queryService,
        ILogger<BaseSearchService> logger) : base(dataService, imageCache, logger)
    {
        _queryService = queryService;
    }

    protected override async Task<SearchResponse> GetQueryImagesAsync( int count = 250)
    {
        int first = _searchResults.Count;

        if ( first < SearchResults.Count() && first + count < SearchResults.Count() )
            // Data already loaded. Nothing to do.
            return new SearchResponse { MoreDataAvailable = false, SearchResults = new int[0] };

        // Calculate how many results we have already
        if ( SearchResults.Count > first )
        {
            var firstOffset = SearchResults.Count - first;
            first = SearchResults.Count;
            count -= firstOffset;
        }

        if ( count == 0 )
            // If we have exactly the right number of results,
            // assume there's more to come
            return new SearchResponse { MoreDataAvailable = true, SearchResults = new int[0] };

        var request = new SearchRequest(Query, first, count);

        Logging.Log($"Executing search query for {request}");

        var response = await _queryService.GetQueryImagesAsync(request);

        _searchResults.AddRange(response.SearchResults);

        return response;
    }
}