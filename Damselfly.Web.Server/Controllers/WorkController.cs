using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using MudBlazor;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Route("/api/work")]
public class WorkController : ControllerBase
{
    private readonly WorkService _service;

    private readonly ILogger<WorkController> _logger;

    public WorkController(WorkService service, ILogger<WorkController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("/api/work")]
    public async Task SetWorkStatus( ServiceStatus newStatus )
    {
        _service.SetStatus(newStatus.StatusText, newStatus.Status, newStatus.CPULevel);
    }
}

