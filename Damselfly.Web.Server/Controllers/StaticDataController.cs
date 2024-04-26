using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/data")]
[AuthorizeFireBase(RoleDefinitions.s_AdminRole)]
public class StaticDataController : ControllerBase
{
    private readonly ILogger<StaticDataController> _logger;
    private readonly MetaDataService _metaDataService;
    private readonly StatisticsService _stats;

    public StaticDataController(MetaDataService metaDataService, StatisticsService stats,
        ILogger<StaticDataController> logger)
    {
        _metaDataService = metaDataService;
        _stats = stats;
        _logger = logger;
    }

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
}