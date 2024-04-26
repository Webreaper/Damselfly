using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsEditor)]
[ApiController]
[Route("/api/tasks")]
[AuthorizeFireBase(RoleDefinitions.s_AdminRole)]
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
        try {
            return await _service.GetTasksAsync();
        }
        catch( Exception ex )
        {
            _logger.LogError($"Unable to retrieve task list: {ex.Message}.");
            return new List<ScheduledTask>();
        }
    }

    [HttpPost("/api/tasks/enqueue")]
    public async Task EnqueueTask(ScheduledTask req)
    {
        await _service.EnqueueTaskAsync(req);
    }
}