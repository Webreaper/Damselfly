using System;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class APIDownloadService : BaseClientService, IDownloadService
{
    public APIDownloadService(HttpClient client) : base(client) { }

    public async Task<string> CreateDownloadZipAsync( ICollection<Image> images, ExportConfig config)
    {
        return string.Empty;
    }
}

