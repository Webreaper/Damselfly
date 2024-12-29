using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/folders")]
public class FolderController : ControllerBase
{
    private readonly ILogger<FolderController> _logger;
    private readonly IFolderService _service;

    public FolderController(FolderService service, ILogger<FolderController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("/api/folders")]
    public async Task<ICollection<Folder>> Get()
    {
        var folders = await _service.GetFolders();

        foreach ( var folder in folders )
        {
            folder.Images.Clear();

            // We clear the children here, and reconstitute them on the client, 
            // to avoid cyclic deserialization issues.

            // TODO: Should probably convert this to use DTOs. 
            folder.Children.Clear();
        }

        return folders;
    }

    [HttpGet("/api/folders/states/{userId}")]
    public async Task<Dictionary<int, UserFolderState>> GetUserFolderStates(int userId)
    {
        return await _service.GetUserFolderStates(userId);
    }

    [HttpPost("/api/folders/state")]
    public async Task UpdateFolderState(IEnumerable<UserFolderState> newStates)
    {
        await _service.SaveFolderStates(newStates);
    }
}