using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/status")]
public class StatusController : ControllerBase
{
    private readonly ILogger<StatusController> _logger;
    private readonly ServerStatusService _statusService;

    public StatusController(ServerStatusService statusService, ILogger<StatusController> logger)
    {
        _statusService = statusService;
        _logger = logger;
    }

    [HttpPost("/api/status")]
    public Task UpdateStatus(StatusUpdateRequest req)
    {
        _statusService.UpdateStatus(req.NewStatus, req.UserId);
        return Task.CompletedTask;
    }
}