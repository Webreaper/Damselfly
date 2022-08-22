using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IDownloadService
{
    Task<string> CreateDownloadZipAsync(ICollection<Image> images, ExportConfig config);
}

