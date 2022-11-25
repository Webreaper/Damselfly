using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface ITaskService
{
    Task<bool> EnqueueTaskAsync(ScheduledTask task);
    Task<List<ScheduledTask>> GetTasksAsync();
}