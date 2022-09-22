using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/work")]
public class WorkController : ControllerBase
{
    private readonly ILogger<WorkController> _logger;
    private readonly WorkService _service;

    public WorkController(WorkService service, ILogger<WorkController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("/api/work/status")]
    public async Task<ServiceStatus> GetStatus()
    {
        return await _service.GetWorkStatus();
    }

    [HttpPost("/api/work/pause")]
    public Task PauseWork(bool paused)
    {
        _service.Paused = paused;
        return Task.CompletedTask;
    }

    [HttpGet("/api/work/settings")]
    public async Task<CPULevelSettings> GetCPUSettings()
    {
        return await _service.GetCPUSchedule();
    }

    [HttpPost("/api/work/settings")]
    public async Task SetCPUSettings(CPULevelSettings newSettings)
    {
        await _service.SetCPUSchedule(newSettings);
    }
}