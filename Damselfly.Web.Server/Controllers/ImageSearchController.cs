using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.Services;
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

    public ImageSearchController( SearchQueryService searchService, ILogger<ImageSearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<SearchResponse> Get()
    {
        var query = new SearchQuery();
        return await _searchService.GetQueryImagesAsync(query, 0, 100);
    }
}

