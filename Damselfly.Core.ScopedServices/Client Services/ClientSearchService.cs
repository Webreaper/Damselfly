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
public class ClientSearchService : BaseSearchService, ISearchService
{
    private readonly RestClient httpClient;
    private readonly IUserStatusService _statusService;

    public ClientSearchService(RestClient client, IUserStatusService statusService,
        ICachedDataService dataService, IImageCacheService imageCache, ILogger<BaseSearchService> logger) :
        base(dataService, imageCache, logger)
    {
        httpClient = client;
        _statusService = statusService;
    }

    protected override async Task<SearchResponse> GetQueryImagesAsync( int count = DamselflyContants.PageSize)
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

            _statusService.UpdateStatus($"Searching for images: {SearchBreadcrumbs}...");

            response = await httpClient.CustomPostAsJsonAsync<SearchRequest, SearchResponse>("/api/search", request);

            if ( response != null )
                if ( response.SearchResults != null && response.SearchResults.Any() )
                {
                    _searchResults.AddRange(response.SearchResults);

                    _statusService.UpdateStatus($"Loaded {response.SearchResults.Count()} search results.");
                }
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Exception during search query API call: {ex}");
        }

        NotifySearchComplete(response);

        return response;
    }

    public void Refresh()
    {
        base.Refresh();
        _statusService.UpdateStatus( "Search results refreshed.");
    }
}