using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;
using System.Threading.Tasks;
using Damselfly.Core.ScopedServices.ClientServices;

namespace Damselfly.Core.ScopedServices;

public class ClientTaskService : ITaskService
{
    private readonly RestClient httpClient;

    public ClientTaskService(RestClient client)
    {
        httpClient = client;
    }

    public async Task<bool> EnqueueTaskAsync(ScheduledTask task)
    {
        await httpClient.CustomPostAsJsonAsync<ScheduledTask>("/api/tasks/enqueue", task);

        return true;
    }

    public async Task<List<ScheduledTask>> GetTasksAsync()
    {
        return await httpClient.CustomGetFromJsonAsync<List<ScheduledTask>>("/api/tasks");
    }
}

