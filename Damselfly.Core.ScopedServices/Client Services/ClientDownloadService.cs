using System;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class ClientDownloadService : IDownloadService
{
    private readonly RestClient httpClient;

    public ClientDownloadService(RestClient client)
    {
        httpClient = client;
    }

    public async Task<DesktopAppPaths> GetDesktopAppInfo()
    {
        return await httpClient.CustomGetFromJsonAsync<DesktopAppPaths>("/api/download/desktopapppaths");
    }

    public async Task<string> CreateDownloadZipAsync(ICollection<Image> images, ExportConfig config)
    {
        return string.Empty;
    }
}

