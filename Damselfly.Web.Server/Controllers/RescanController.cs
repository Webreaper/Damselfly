using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
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
[Route("/api/rescan")]
public class RescanController : ControllerBase
{
    private readonly IRescanService _rescanService;

    private readonly ILogger<RescanController> _logger;

    public RescanController( IRescanService rescanService, ILogger<RescanController> logger)
    {
        _rescanService = rescanService;
        _logger = logger;
    }

    [HttpPost( "/api/rescan/clearfaces" )]
    public async Task UpdateStatus()
    {
        await _rescanService.ClearFaceThumbs();
    }

    [HttpPost("/api/rescan")]
    public async Task UpdateStatus(RescanRequest req)
    {
        if( req.RescanAll )
            await _rescanService.MarkAllForRescan( req.ScanType );
        else if( req.FolderId.HasValue )
            await _rescanService.MarkFolderForRescan( req.ScanType, req.FolderId.Value );
        else if( req.ImageIds != null && req.ImageIds.Any() )
            await _rescanService.MarkImagesForRescan( req.ScanType, req.ImageIds );
        else
            throw new ArgumentException( "Unexpected or invalid rescan request payload!" );
    }
}

