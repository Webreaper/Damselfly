using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;
using System.Threading.Tasks;

namespace Damselfly.Core.ScopedServices;

public class ClientTaskService : BaseClientService, ITaskService
{
    public ClientTaskService(HttpClient client) : base(client) { }

    public async Task<bool> EnqueueTaskAsync(ScheduledTask task)
    {
        await httpClient.PostAsJsonAsync<ScheduledTask>("/api/tasks/enqueue", task);

        return true;
    }

    public async Task<List<ScheduledTask>> GetTasksAsync()
    {
        return await httpClient.GetFromJsonAsync<List<ScheduledTask>>("/api/tasks");
    }
}

