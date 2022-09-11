using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Humanizer;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.Http.Json;
using System.Net.Http;
using Damselfly.Core.ScopedServices.Interfaces;
using System.Text.Json;
using Damselfly.Core.ScopedServices.ClientServices;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// The client search service is used in WASM. It calls the server-side API to query
/// search service there. 
/// </summary>
public class ClientSearchService : BaseSearchService, ISearchService, IDisposable
{
    private readonly RestClient httpClient;
    private readonly ICachedDataService _dataService;

    public ClientSearchService(RestClient client, ICachedDataService dataService, ILogger<BaseSearchService> logger) : base(dataService, logger)
    {
        httpClient = client;
        _dataService = dataService;

        OnSearchChanged += ClearSearchResults;
    }

    public override async Task<SearchResponse> GetQueryImagesAsync(int first, int count)
    {
        _logger.LogTrace($"Running search query for {first}...{first + count}");

        if (first < SearchResults.Count() && first + count < SearchResults.Count())
        {
            // Data already loaded. Nothing to do.
            return new SearchResponse { MoreDataAvailable = false, SearchResults = new int[0] };
        }

        // Calculate how many results we have already
        if (SearchResults.Count() >= first)
        {
            int firstOffset = SearchResults.Count() - first;
            first = SearchResults.Count();
            count -= firstOffset;
        }

        if (count == 0)
        {
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

        var response = await httpClient.CustomPostAsJsonAsync<SearchRequest, SearchResponse>("/api/search", request);

        // WASM: should this just get added into the navigation manager directly?
        _searchResults.AddRange(response.SearchResults);

        return response;
    }

    public void Dispose()
    {
        OnSearchChanged -= ClearSearchResults;
    }
}
