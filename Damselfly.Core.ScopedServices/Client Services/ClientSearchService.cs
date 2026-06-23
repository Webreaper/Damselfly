using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

/// <summary>
///     The client search service is used in WASM. It calls the server-side API to query
///     search service there.
/// </summary>
public class ClientSearchService (
    RestClient client,
    IUserStatusService statusService,
    ICachedDataService dataService,
    IImageCacheService imageCache,
    ILogger<BaseSearchService> logger)
    : BaseSearchService(dataService, imageCache, logger), ISearchService
{
    protected override async Task<SearchResponse> GetQueryImagesAsync( CancellationToken token, int count = DamselflyContants.PageSize)
    {
        var first = SearchResults.Count;
        var response = new SearchResponse { MoreDataAvailable = false, SearchResults = new int[0] };

        try
        {
            if ( first < SearchResults.Count() && first + count < SearchResults.Count() )
                // Data already loaded. Nothing to do.
                return new SearchResponse { MoreDataAvailable = false, SearchResults = new int[0] };

            // Calculate how many results we have already
            if ( SearchResults.Count() >= first )
            {
                var firstOffset = SearchResults.Count() - first;
                first = SearchResults.Count();
                count -= firstOffset;
            }

            if ( count == 0 )
                // If we have exactly the right number of results,
                // assume there's more to come
                return new SearchResponse { MoreDataAvailable = true, SearchResults = new int[0] };

            var request = new SearchRequest(Query, first, count);

            _logger.LogInformation(
                $"Executing search for {SearchBreadcrumbs} ({SearchResults.Count} results were already loaded.");

            statusService.UpdateStatus($"Searching for images: {SearchBreadcrumbs}...");

            response = await client.CustomPostAsJsonAsync<SearchRequest, SearchResponse>("/api/search", request, token);

            if ( response != null && response.SearchResults.Any() )
            {
                _searchResults.AddRange(response.SearchResults);

                statusService.UpdateStatus($"Loaded {response.SearchResults.Count()} search results.");
            }
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Exception during search query API call: {ex}");
        }

        NotifySearchComplete(response);

        return response;
    }
    
    public override void Refresh()
    {
        base.Refresh();
        statusService.UpdateStatus( "Search results refreshed.");
    }
}