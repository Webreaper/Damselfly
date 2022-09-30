using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsEditor)]
[ApiController]
[Route("/api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ILogger<TasksController> _logger;
    private readonly ITaskService _service;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _service = taskService;
        _logger = logger;
    }

    [HttpGet("/api/tasks")]
    public async Task<List<ScheduledTask>> GetTasks()
    {
        return await _service.GetTasksAsync();
    }

    [HttpPost("/api/tasks/enqueue")]
    public async Task EnqueueTask(ScheduledTask req)
    {
        await _service.EnqueueTaskAsync(req);
    }
}