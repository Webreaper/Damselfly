using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/files")]
public class FileController : ControllerBase
{
    private readonly ILogger<FileController> _logger;
    private readonly IFileService _service;

    public FileController( IFileService service, ILogger<FileController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost( "/api/files/delete" )]
    public async Task<bool> DeleteImages( MultiImageRequest req )
    {
        return await _service.DeleteImages( req );
    }

    [HttpPost("/api/files/move")]
    public async Task<bool> MoveImages( ImageMoveRequest req)
    {
        return await _service.MoveImages(req);
    }
}