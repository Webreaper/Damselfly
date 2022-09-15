using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
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

