using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.Services;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/search")]
[Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
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
            return new SearchResponse { MoreDataAvailable = false, SearchResults = new Guid[0] };
        }
    }
}