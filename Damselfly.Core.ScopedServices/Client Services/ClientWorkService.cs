using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class ClientWorkService : IWorkService
{
    private readonly NotificationsService _notifications;
    private readonly RestClient httpClient;

    public ClientWorkService(RestClient client, NotificationsService notifications)
    {
        httpClient = client;
        _notifications = notifications;

        _notifications.SubscribeToNotification<ServiceStatus>(NotificationType.WorkStatusChanged, NotifyStatusChanged);
    }

    public event Action<ServiceStatus> OnStatusChanged;

    public async Task Pause(bool paused)
    {
        await httpClient.CustomPostAsJsonAsync("/api/work/pause", paused);
    }

    public async Task<ServiceStatus> GetWorkStatus()
    {
        return await httpClient.CustomGetFromJsonAsync<ServiceStatus>("/api/work/status");
    }

    public async Task<CPULevelSettings> GetCPUSchedule()
    {
        return await httpClient.CustomGetFromJsonAsync<CPULevelSettings>("/api/work/settings");
    }

    public async Task SetCPUSchedule(CPULevelSettings settings)
    {
        await httpClient.CustomPostAsJsonAsync("/api/work/settings", settings);
    }

    private void NotifyStatusChanged(ServiceStatus newStatus)
    {
        OnStatusChanged?.Invoke(newStatus);
    }
}