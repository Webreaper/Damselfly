using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsDownloader)]
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

    [HttpPost("/api/download/images")]
    public async Task<DownloadResponse> GetImagesDownload(DownloadRequest req)
    {
        var url = await _downloadService.CreateDownloadZipAsync(req.ImageIds, req.Config);
        return new DownloadResponse { DownloadUrl = url };
    }
}