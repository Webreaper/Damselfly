using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.ClientServices;

namespace Damselfly.Core.ScopedServices;

public class ClientExportService
{
    private readonly RestClient httpClient;

    public ClientExportService(RestClient client)
    {
        httpClient = client;
    }

    public async Task Delete(ExportConfig config)
    {
        await httpClient.CustomDeleteAsync($"/api/export/config/{config.ExportConfigId}");
    }

    public async Task Save(ExportConfig config)
    {
        await httpClient.CustomPatchAsJsonAsync<ExportConfig>($"/api/export/config", config);
    }

    public async Task Create(ExportConfig config)
    {
        await httpClient.CustomPutAsJsonAsync<ExportConfig>($"/api/export/config", config);
    }
}

