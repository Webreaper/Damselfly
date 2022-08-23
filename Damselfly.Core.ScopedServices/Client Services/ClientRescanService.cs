using System;
using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class ClientRescanService : IRescanService
{
    private readonly RestClient httpClient;

    public ClientRescanService( RestClient client) 
    {
        httpClient = client;       
    }

    public async Task ClearFaceThumbs()
    {
        throw new NotImplementedException("To be done");
    }

    public async Task MarkFolderForRescan( RescanTypes rescanType, Folder folder )
    {
        throw new NotImplementedException("To be done");
    }

    public async Task MarkImagesForRescan(RescanTypes rescanType, ICollection<Image> images)
    {
        throw new NotImplementedException("To be done");
    }

    public async Task MarkAllForRescan(RescanTypes rescanType)
    {
        throw new NotImplementedException("To be done");
    }
}

