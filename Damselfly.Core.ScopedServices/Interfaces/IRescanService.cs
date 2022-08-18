using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IRescanService
{
    Task MarkFolderForRescan(RescanTypes rescanType, Folder folder);
    Task MarkImagesForRescan(RescanTypes rescanType, ICollection<Image> images);
    Task MarkAllForRescan(RescanTypes rescanType);
}

public interface IRescanProvider
{
    Task MarkFolderForScan(Folder folder);
    Task MarkImagesForScan(ICollection<Image> images);
    Task MarkAllForScan();
}
