using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.ScopedServices.ClientServices;

namespace Damselfly.Core.ScopedServices;

public class ClientWorkService : IWorkService
{
    private readonly RestClient httpClient;
    private readonly NotificationsService _notifications;

    public ClientWorkService(RestClient client, NotificationsService notifications)
    {
        httpClient = client;
        _notifications = notifications;

        _notifications.SubscribeToNotification<ServiceStatus>(NotificationType.WorkStatusChanged, NotifyStatusChanged);
    }

    private void NotifyStatusChanged( ServiceStatus newStatus )
    {
        OnStatusChanged?.Invoke(newStatus);
    }

    // WASM: TODO: 
    public event Action<ServiceStatus> OnStatusChanged;

    public async Task Pause(bool paused)
    {
        await httpClient.CustomPostAsJsonAsync($"/api/work/pause", paused);
    }

    public async Task<ServiceStatus> GetWorkStatus()
    {
        return await httpClient.CustomGetFromJsonAsync<ServiceStatus>("/api/work/status");
    }
}

