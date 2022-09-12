using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.ScopedServices.ClientServices;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// The client search service is used in WASM. It calls the server-side API to query
/// search service there. 
/// </summary>
public class ClientSearchService : BaseSearchService, ISearchService
{
    private readonly RestClient httpClient;

    public ClientSearchService(RestClient client, ICachedDataService dataService, ILogger<BaseSearchService> logger) : base(dataService, logger)
    {
        httpClient = client;
    }

    public override async Task<SearchResponse> GetQueryImagesAsync(int first, int count)
    {
        try
        {
            if ( first < SearchResults.Count() && first + count < SearchResults.Count() )
            {
                _logger.LogInformation( $"ImageSearch: => No results found." );
                // Data already loaded. Nothing to do.
                return new SearchResponse { MoreDataAvailable = false, SearchResults = new int[0] };
            }

            // Calculate how many results we have already
            if ( SearchResults.Count() >= first )
            {
                int firstOffset = SearchResults.Count() - first;
                first = SearchResults.Count();
                count -= firstOffset;
            }

            if ( count == 0 )
            {
                _logger.LogInformation( $"ImageSearch: => No more images needed." );

                // If we have exactly the right number of results,
                // assume there's more to come
                return new SearchResponse { MoreDataAvailable = true, SearchResults = new int[0] };
            }

            var request = new SearchRequest
            {
                Query = this.Query,
                First = first,
                Count = count
            };

            _logger.LogInformation( $"ImageSearch: Calling search API query for {request}" );

            var response = await httpClient.CustomPostAsJsonAsync<SearchRequest, SearchResponse>( "/api/search", request );

            if ( response != null )
            {
                if ( response.SearchResults != null && response.SearchResults.Any() )
                {
                    _searchResults.AddRange( response.SearchResults );
                    _logger.LogInformation( $"ImageSearch: => Found {response.SearchResults.Count()} results." );
                }
                else
                    _logger.LogWarning( "ImageSearch: NULL or empty search results returned from search API" );

                return response;
            }
            else
                _logger.LogError( "ImageSearch: NULL response returned from search API" );
        }
        catch ( Exception ex )
        {
            _logger.LogError( $"Exception during search query API call: {ex}" );
        }

        return new SearchResponse { MoreDataAvailable = false, SearchResults = new int[0] };
    }
}
