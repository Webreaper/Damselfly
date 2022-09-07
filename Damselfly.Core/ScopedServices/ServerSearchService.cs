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
using Damselfly.Core.Services;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// The search service manages the current set of parameters that make up the search
/// query, and thus determine the set of results returned to the image browser list.
/// The results are stored here, and returned as a virtualised set - so we only pass
/// back (say) 200 images, and then requery for the next 200 when the user scrolls.
/// This saves us returning thousands of items for a search.
/// </summary>
public class ServerSearchService : BaseSearchService, ISearchService
{
    private SearchQueryService _queryService;

    public ServerSearchService(ICachedDataService dataService, SearchQueryService queryService, ILogger<BaseSearchService> logger) : base(dataService, logger)
    {
        _queryService = queryService;
    }

    public override async Task<SearchResponse> GetQueryImagesAsync(int first, int count)
    {
        if (first < SearchResults.Count() && first + count < SearchResults.Count())
        {
            // Data already loaded. Nothing to do.
            return new SearchResponse { MoreDataAvailable = false, SearchResults = new int[0] };
        }

        // Calculate how many results we have already
        if (SearchResults.Count > first)
        {
            int firstOffset = SearchResults.Count - first;
            first = SearchResults.Count;
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

        var response = await _queryService.GetQueryImagesAsync(request);

        // WASM: should this just get added into the navigation manager directly?
        _searchResults.AddRange(response.SearchResults);

        return response;
    }
}
