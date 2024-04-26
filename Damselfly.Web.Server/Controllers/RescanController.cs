using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsEditor)]
[ApiController]
[Route("/api/rescan")]
[AuthorizeFireBase(RoleDefinitions.s_AdminRole)]
public class RescanController : ControllerBase
{
    private readonly ILogger<RescanController> _logger;
    private readonly IRescanService _rescanService;

    public RescanController(IRescanService rescanService, ILogger<RescanController> logger)
    {
        _rescanService = rescanService;
        _logger = logger;
    }

    [HttpPost("/api/rescan/clearfaces")]
    public async Task UpdateStatus()
    {
        await _rescanService.ClearFaceThumbs();
    }

    [HttpPost("/api/rescan")]
    public async Task UpdateStatus(RescanRequest req)
    {
        if ( req.RescanAll )
            await _rescanService.MarkAllForRescan(req.ScanType);
        else if ( req.FolderId.HasValue )
            await _rescanService.MarkFolderForRescan(req.ScanType, req.FolderId.Value);
        else if ( req.ImageIds != null && req.ImageIds.Any() )
            await _rescanService.MarkImagesForRescan(req.ScanType, req.ImageIds);
        else
            throw new ArgumentException("Unexpected or invalid rescan request payload!");
    }
}