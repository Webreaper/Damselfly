using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.Services;


public class RescanService : IRescanService
{
    private readonly ThumbnailService _thumbService;

    public RescanService(ThumbnailService thumbService)
    {
        _thumbService = thumbService;
    }

    private IRescanProvider GetService(RescanTypes type)
    {
        return type switch
        {
            RescanTypes.Thumbnails => _thumbService,
            _ => throw new ArgumentException($"Unknown rescan service {type}")
        };
    }

    public async Task MarkAllForRescan(RescanTypes rescanType)
    {
        var provider = GetService(rescanType);

        await provider.MarkAllForScan();
    }

    public async Task MarkFolderForRescan(RescanTypes rescanType, int folderId)
    {
        var provider = GetService(rescanType);

        await provider.MarkFolderForScan( folderId );
    }

    public async Task MarkImagesForRescan(RescanTypes rescanType, ICollection<int> imageIds)
    {
        var provider = GetService(rescanType);

        await provider.MarkImagesForScan( imageIds );
    }

    public async Task ClearFaceThumbs()
    {
        await Task.Run(() => _thumbService.ClearFaceThumbs());
    }
}

