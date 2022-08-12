using System;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class APIDownloadService : IDownloadService
{
    public async Task<string> CreateDownloadZipAsync( ICollection<Image> images, ExportConfig config)
    {
        return string.Empty;
    }
}

