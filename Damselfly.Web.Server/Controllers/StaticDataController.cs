using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Route("/api/data")]
public class StaticDataController : ControllerBase
{
    private readonly MetaDataService _metaDataService;
    private readonly StatisticsService _stats;

    private readonly ILogger<StaticDataController> _logger;

    public StaticDataController(MetaDataService metaDataService, StatisticsService stats, ILogger<StaticDataController> logger)
    {
        _metaDataService = metaDataService;
        _stats = stats;
        _logger = logger;
    }

    [HttpGet( "/api/data/static" )]
    public Task<StaticData> GetStaticData()
    {
        return Task.FromResult( new StaticData
        {
            ExifToolVer = ExifService.ExifToolVer,
            ImagesRootFolder = IndexingService.RootFolder
        } );
    }

    [HttpGet("/api/data/cameras")]
    public Task<ICollection<Camera>> GetCameras()
    {
        ICollection<Camera> result = _metaDataService.Cameras;
        return Task.FromResult( result );
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
}

