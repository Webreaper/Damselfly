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
[Route("/api/folders")]
public class FolderController : ControllerBase
{
    private readonly FolderService _service;

    private readonly ILogger<FolderController> _logger;

    public FolderController(FolderService service, ILogger<FolderController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("/api/folders")]
    public async Task<ICollection<Folder>> Get()
    {
        var folders = await _service.GetFolders();
        return folders;
    }
}

