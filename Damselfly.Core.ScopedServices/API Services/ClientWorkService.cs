using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;

namespace Damselfly.Core.ScopedServices;

public class ClientWorkService : BaseClientService
{
    public ClientWorkService(HttpClient client) : base(client) { }

    // WASM: TODO: 
    public event Action<ServiceStatus> OnStatusChanged;

    public async Task<HttpResponseMessage> SetWorkStatus(ServiceStatus newStatus)
    {
        return await httpClient.PostAsJsonAsync("/api/work", newStatus);
    }
}

