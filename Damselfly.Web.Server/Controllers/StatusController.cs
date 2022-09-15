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
[Route("/api/status")]
public class StatusController : ControllerBase
{
    private readonly ServerStatusService _statusService;

    private readonly ILogger<StatusController> _logger;

    public StatusController(ServerStatusService statusService, ILogger<StatusController> logger)
    {
        _statusService = statusService;
        _logger = logger;
    }

    [HttpPost("/api/status")]
    public async Task UpdateStatus(StatusUpdateRequest req)
    {
        await _statusService.UpdateStatus(req.NewStatus, req.UserId);
    }
}

