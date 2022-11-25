using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/tags")]
public class TagController : ControllerBase
{
    private readonly ILogger<TagController> _logger;
    private readonly IRecentTagService _recentTagService;
    private readonly ITagSearchService _tagSearch;
    private readonly ITagService _tagService;

    public TagController(ITagService tagService, IRecentTagService recentTagService, ITagSearchService tagSearchService,
        ILogger<TagController> logger)
    {
        _tagService = tagService;
        _tagSearch = tagSearchService;
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

    [HttpGet("/api/tags/search/{filterText}")]
    public async Task<ICollection<Tag>> SearchTags(string filterText)
    {
        return await _tagSearch.SearchTags(filterText);
    }

    [HttpGet("/api/tag/{tagId}")]
    public async Task<Tag> GetTag( int tagId )
    {
        return await _tagSearch.GetTag( tagId );
    }

    [HttpGet("/api/tags")]
    public async Task<ICollection<Tag>> GetAllTags()
    {
        return await _tagSearch.GetAllTags();
    }

    [HttpPost("/api/tags")]
    public async Task UpdateTags(TagUpdateRequest req)
    {
        await _tagService.UpdateTagsAsync(req.ImageIDs, req.TagsToAdd, req.TagsToDelete, req.UserId);
    }

    [HttpPost("/api/tags/exif")]
    public async Task UpdateTags(ExifUpdateRequest req)
    {
        await _tagService.SetExifFieldAsync(req.ImageIDs, req.ExifType, req.NewValue, req.UserId);
    }

    [HttpPost("/api/tags/togglefave")]
    public async Task<bool> ToggleFavourite(Tag tag)
    {
        return await _tagService.ToggleFavourite(tag);
    }
}