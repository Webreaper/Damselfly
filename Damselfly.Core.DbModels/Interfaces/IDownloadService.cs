using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IDownloadService
{
    Task<string> CreateDownloadZipAsync(ICollection<int> images, ExportConfig config);
    Task<DesktopAppPaths> GetDesktopAppInfo();
}

