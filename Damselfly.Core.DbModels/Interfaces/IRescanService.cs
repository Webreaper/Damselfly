using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Constants;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IRescanService
{
    Task MarkFolderForRescan(RescanTypes rescanType, Guid folderId);
    Task MarkImagesForRescan(RescanTypes rescanType, ICollection<Guid> imageIds);
    Task MarkAllForRescan(RescanTypes rescanType);

    Task ClearFaceThumbs();
}

public interface IRescanProvider
{
    Task MarkFolderForScan(Guid folderId);
    Task MarkImagesForScan(ICollection<Guid> imageIds);
    Task MarkAllForScan();
}