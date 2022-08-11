using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Web.Client.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ImageController : ControllerBase
{
    [Inject]
    protected SearchService searchService { get; set; }

    private readonly ILogger<ImageController> _logger;

    public ImageController( ILogger<ImageController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IEnumerable<Image>> Get()
    {
        var response = await searchService.GetQueryImagesAsync(1, 100);

        return response.SearchResults;
    }
}

