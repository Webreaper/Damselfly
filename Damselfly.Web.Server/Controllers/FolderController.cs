using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Microsoft.AspNetCore.Mvc.Route("/api/folders")]
public class FolderController : ControllerBase
{
    private readonly ILogger<FolderController> _logger;
    private readonly FolderService _service;

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
            folder.Images.Clear();

        return folders;
    }
}