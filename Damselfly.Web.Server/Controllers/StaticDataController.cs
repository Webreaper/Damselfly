using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/data")]
public class StaticDataController( ILogger<StaticDataController> _logger, 
                                MetaDataService _metaDataService,
                                ICachedDataService _cachedData,
                                StatisticsService _stats) : ControllerBase
{
    [HttpGet("/api/data/static")]
    public Task<StaticData> GetStaticData()
    {
        return Task.FromResult(new StaticData
        {
            ExifToolVer = ExifService.ExifToolVer,
            ImagesRootFolder = IndexingService.RootFolder
        });
    }

    [HttpGet("/api/data/cameras")]
    public Task<ICollection<Camera>> GetCameras()
    {
        ICollection<Camera> result = _metaDataService.Cameras;
        return Task.FromResult(result);
    }

    [HttpGet("/api/data/lenses")]
    public Task<ICollection<Lens>> GetLenses()
    {
        ICollection<Lens> result = _metaDataService.Lenses;
        return Task.FromResult(result);
    }

    [HttpGet("/api/data/stats")]
    public async Task<Statistics> GetStatistics()
    {
        return await _stats.GetStatistics();
    }

    [HttpGet("/api/data/newversion")]
    public async Task<NewVersionResponse> CheckForNewVersion()
    {
        return await _cachedData.CheckForNewVersion();
    }
}