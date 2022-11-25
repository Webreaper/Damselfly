using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;

namespace Damselfly.Core.ScopedServices;

public class CachedDataService : ICachedDataService
{
    private readonly MetaDataService _metaDataService;
    private readonly StatisticsService _stats;

    public CachedDataService(MetaDataService metaDataService, StatisticsService stats)
    {
        _stats = stats;
        _metaDataService = metaDataService;
    }

    public string ImagesRootFolder => IndexingService.RootFolder;

    public string ExifToolVer => ExifService.ExifToolVer;

    public ICollection<Camera> Cameras => _metaDataService.Cameras;

    public ICollection<Lens> Lenses => _metaDataService.Lenses;

    public Task InitialiseData()
    {
        // Nothng to do here in the Blazor Server version
        return Task.CompletedTask;
    }

    public async Task<Statistics> GetStatistics()
    {
        return await _stats.GetStatistics();
    }

    public Task ClearCache()
    {
        // No-op
        return Task.CompletedTask;
    }
}