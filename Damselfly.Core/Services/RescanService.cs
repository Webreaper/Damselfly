using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.Services;

public class RescanService : IRescanService
{
    private readonly IndexingService _indexingService;
    private readonly MetaDataService _metaDataService;
    private readonly ThumbnailService _thumbService;

    public RescanService(ThumbnailService thumbService, IndexingService indexingService,
        MetaDataService metaDataService)
    {
        _thumbService = thumbService;
        _indexingService = indexingService;
        _metaDataService = metaDataService;
    }

    public async Task MarkAllForRescan(RescanTypes rescanType)
    {
        var providers = GetService(rescanType);

        await Task.WhenAll(providers.Select(x => x.MarkAllForScan()));
    }

    public async Task MarkFolderForRescan(RescanTypes rescanType, Guid folderId)
    {
        var providers = GetService(rescanType);

        await Task.WhenAll(providers.Select(x => x.MarkFolderForScan(folderId)));
    }

    public async Task MarkImagesForRescan(RescanTypes rescanType, ICollection<Guid> imageIds)
    {
        var providers = GetService(rescanType);

        await Task.WhenAll(providers.Select(x => x.MarkImagesForScan(imageIds)));
    }

    public async Task ClearFaceThumbs()
    {
        await Task.Run(() => _thumbService.ClearFaceThumbs());
    }

    private ICollection<IRescanProvider> GetService(RescanTypes type)
    {
        var providers = new List<IRescanProvider>();

        if ( type.HasFlag(RescanTypes.Thumbnails) )
            providers.Add(_thumbService);
        if ( type.HasFlag(RescanTypes.Metadata) )
            providers.Add(_metaDataService);
        if ( type.HasFlag(RescanTypes.Indexing) )
            providers.Add(_indexingService);

        if ( providers.Count() != BitOperations.PopCount((ulong)type) )
            throw new ArgumentException($"Unknown rescan service {type}");

        return providers;
    }
}