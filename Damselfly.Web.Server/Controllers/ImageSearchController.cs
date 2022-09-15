using Damselfly.Core.DbModels;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Microsoft.AspNetCore.Mvc.Route("/api/search")]
public class ImageSearchController : ControllerBase
{
    private readonly ILogger<ImageSearchController> _logger;
    private readonly SearchQueryService _searchService;

    public ImageSearchController(SearchQueryService searchService, ILogger<ImageSearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpPost("/api/search")]
    public async Task<SearchResponse> SubmitSearch(SearchRequest request)
    {
        try
        {
            return await _searchService.GetQueryImagesAsync(request);
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Exception during search query: {ex}");
            return new SearchResponse { MoreDataAvailable = false, SearchResults = new int[0] };
        }
    }
}