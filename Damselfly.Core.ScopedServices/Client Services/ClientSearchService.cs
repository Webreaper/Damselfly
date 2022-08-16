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

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// The client search service is used in WASM. It calls the server-side API to query
/// search service there. 
/// </summary>
public class ClientSearchService : BaseSearchService, ISearchService
{
    public ClientSearchService(HttpClient client, ICachedDataService dataService) : base(client, dataService) { }

    public override async Task<SearchResponse> GetQueryImagesAsync( int start, int count )
    {
        var request = new SearchRequest
        {
            Query = this.Query,
            First = start,
            Count = count
        };
        var response = await httpClient.PostAsJsonAsync("/api/search", request );

        return await response.Content.ReadFromJsonAsync<SearchResponse>();
    }
}
