using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Route("/api/tags")]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly IRecentTagService _recentTagService;

    private readonly ILogger<TagController> _logger;

    public TagController(ITagService tagService, IRecentTagService recentTagService, ILogger<TagController> logger)
    {
        _tagService = tagService;
        _recentTagService = recentTagService;
        _logger = logger;
    }

    [HttpGet("/api/tags/favourites")]
    public async Task<ICollection<Tag>> GetFavourites()
    {
        return await _tagService.GetFavouriteTags();
    }

    [HttpGet("/api/tags/recents")]
    public async Task<ICollection<string>> GetRecents()
    {
        return await _recentTagService.GetRecentTags();
    }
}

