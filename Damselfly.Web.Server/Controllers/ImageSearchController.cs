using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.Services;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Route("/api/search")]
public class ImageSearchController : ControllerBase
{
    private readonly SearchQueryService _searchService;

    private readonly ILogger<ImageSearchController> _logger;

    public ImageSearchController(SearchQueryService searchService, ILogger<ImageSearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpPost( "/api/search" )]
    public async Task<SearchResponse> SubmitSearch( SearchRequest request )
    {
        try
        {
            return await _searchService.GetQueryImagesAsync( request );
        }
        catch( Exception ex )
        {
            _logger.LogError( $"Exception during search query: {ex}" );
            return new SearchResponse { MoreDataAvailable = false, SearchResults = new int[0]  };
        }
    }
}

