using System;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class ClientDownloadService : BaseClientService, IDownloadService
{
    public ClientDownloadService(HttpClient client) : base(client) { }

    public async Task<string> CreateDownloadZipAsync( ICollection<Image> images, ExportConfig config)
    {
        return string.Empty;
    }
}

