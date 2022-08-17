using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class ClientWorkService : BaseClientService, IWorkService
{
    public ClientWorkService(HttpClient client) : base(client) { }

    // WASM: TODO: 
    public event Action<ServiceStatus> OnStatusChanged;

    public async Task Pause(bool paused)
    {
        await httpClient.PostAsJsonAsync($"/api/work/pause", paused);
    }

    public async Task<ServiceStatus> GetWorkStatus()
    {
        return await httpClient.GetFromJsonAsync<ServiceStatus>("/api/work/status");
    }
}

