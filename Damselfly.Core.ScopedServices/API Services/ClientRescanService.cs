using System;
using Damselfly.Core.Constants;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices;

public class ClientRescanService : BaseClientService
{
    public ClientRescanService(HttpClient client) : base(client) { }

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

