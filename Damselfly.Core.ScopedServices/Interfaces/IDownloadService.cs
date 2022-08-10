using System;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IDownloadService
{
    Task<string> CreateDownloadZipAsync(ICollection<Image> images, ExportConfig config);
}

