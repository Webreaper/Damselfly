using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IRescanService
{
    Task MarkFolderForRescan(RescanTypes rescanType, int folderId);
    Task MarkImagesForRescan(RescanTypes rescanType, ICollection<int> imageIds);
    Task MarkAllForRescan(RescanTypes rescanType);

    Task ClearFaceThumbs();
}

public interface IRescanProvider
{
    Task MarkFolderForScan(int folderId);
    Task MarkImagesForScan(ICollection<int> imageIds);
    Task MarkAllForScan();

}
