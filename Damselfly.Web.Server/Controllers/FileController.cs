using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/files")]
[Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
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