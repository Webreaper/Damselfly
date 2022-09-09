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
[Route("/api/download")]
public class DownloadController : ControllerBase
{
    private readonly IDownloadService _downloadService;

    private readonly ILogger<DownloadController> _logger;

    public DownloadController(IDownloadService service, ILogger<DownloadController> logger)
    {
        _downloadService = service;
        _logger = logger;
    }

    [HttpGet("/api/download/desktopapppaths")]
    public async Task<DesktopAppPaths> DesktopAppPaths()
    {
        return await _downloadService.GetDesktopAppInfo();
    }

    [HttpPost( "/api/download/images" )]
    public async Task<DownloadResponse> GetImagesDownload( DownloadRequest req )
    {
        var url = await _downloadService.CreateDownloadZipAsync( req.ImageIds, req.Config );
        return new DownloadResponse {  DownloadUrl = url };
    }
}

