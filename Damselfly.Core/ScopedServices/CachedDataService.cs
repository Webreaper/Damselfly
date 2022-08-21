using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;

namespace Damselfly.Core.ScopedServices;

public class CachedDataService : ICachedDataService
{
    public CachedDataService( MetaDataService metaDataService )
    {
        _metaDataService = metaDataService;
    }

    private MetaDataService _metaDataService;

    public string ImagesRootFolder => IndexingService.RootFolder;

    public string ExifToolVer => ExifService.ExifToolVer;

    public ICollection<Camera> Cameras => _metaDataService.Cameras;

    public ICollection<Lens> Lenses => _metaDataService.Lenses;

    public async Task InitialiseData()
    {
        // Nothng to do here in the Blazor Server version
    }
}

