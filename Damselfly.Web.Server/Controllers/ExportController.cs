using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsDownloader)]
[ApiController]
[Route("/api/export")]
public class ExportController : ControllerBase
{
    private readonly ImageRecognitionService _aiService;

    private readonly ILogger<ExportController> _logger;

    public ExportController(ImageRecognitionService service, ILogger<ExportController> logger)
    {
        _aiService = service;
        _logger = logger;
    }

    [HttpDelete("/api/export/config/{configId}")]
    public async Task DeleteConfig(int configId, [FromServices] ImageContext db)
    {
        var existingConfig = db.DownloadConfigs.Where(x => x.ExportConfigId == configId);

        await db.BatchDelete(existingConfig);
    }

    [HttpGet("/api/export/configs")]
    public async Task<ICollection<ExportConfig>> GetExportConfigs([FromServices] ImageContext db)
    {
        return await db.DownloadConfigs.ToListAsync();
    }

    [HttpPatch("/api/export/config")]
    public async Task UpdateConfig(ExportConfig config, [FromServices] ImageContext db)
    {
        try
        {
            db.DownloadConfigs.Update(config);
            await db.SaveChangesAsync("SaveExportConfig");
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Unexpected error saving export config: {ex}");
            throw;
        }
    }

    [HttpPut("/api/export/config")]
    public async Task CreateConfig(ExportConfig config, [FromServices] ImageContext db)
    {
        try
        {
            if ( db.DownloadConfigs.Any(x => x.Name.Equals(config.Name)) )
                throw new ArgumentException($"Config {config.Name} already exists!");

            db.DownloadConfigs.Add(config);
            await db.SaveChangesAsync("SaveExportConfig");
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Unexpected error saving export config: {ex}");
            throw;
        }
    }
}