using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
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

    public async Task<string> CreateDownloadZipAsync(ICollection<int> imageIds, ExportConfig config)
    {
        var request = new DownloadRequest { ImageIds = imageIds, Config = config };

        var response =
            await httpClient.CustomPostAsJsonAsync<DownloadRequest, DownloadResponse>("/api/download/images", request);

        if( response != null )
            return response.DownloadUrl;

        return null;
    }
}