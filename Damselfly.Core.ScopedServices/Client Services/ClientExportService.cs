using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;

namespace Damselfly.Core.ScopedServices;

public class ClientExportService : BaseClientService
{
    public ClientExportService(HttpClient client) : base(client) { }

    public async Task Delete( ExportConfig config )
    {
        await httpClient.DeleteAsync($"/api/export/config/{config.ExportConfigId}" );
    }

    public async Task Save(ExportConfig config)
    {
        await httpClient.PatchAsJsonAsync<ExportConfig>($"/api/export/config", config);
    }

    public async Task Create(ExportConfig config)
    {
        await httpClient.PutAsJsonAsync<ExportConfig>($"/api/export/config", config);
    }
}

