using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class ClientRescanService : IRescanService
{
    private readonly RestClient httpClient;

    public ClientRescanService(RestClient client)
    {
        httpClient = client;
    }

    public async Task ClearFaceThumbs()
    {
        await httpClient.CustomPostAsJsonAsync( "/api/rescan/clearfaces", true );
    }

    public async Task MarkFolderForRescan(RescanTypes rescanType, int folderId)
    {
        var req = new RescanRequest { ScanType = rescanType, FolderId = folderId };
        await httpClient.CustomPostAsJsonAsync( "/api/rescan", req );
    }

    public async Task MarkImagesForRescan(RescanTypes rescanType, ICollection<int> imageIds)
    {
        var req = new RescanRequest { ScanType = rescanType, ImageIds = imageIds };
        await httpClient.CustomPostAsJsonAsync( "/api/rescan", req );
    }

    public async Task MarkAllForRescan(RescanTypes rescanType)
    {
        var req = new RescanRequest { ScanType = rescanType, RescanAll = true };
        await httpClient.CustomPostAsJsonAsync( "/api/rescan", req );
    }
}

