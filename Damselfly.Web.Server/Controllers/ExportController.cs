using Damselfly.Core.DbModels;
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
    public async Task DeleteConfig(int configId)
    {
        using var db = new ImageContext();

        var existingConfig = db.DownloadConfigs.Where(x => x.ExportConfigId == configId );

        await db.BatchDelete(existingConfig);
    }

    [HttpPatch("/api/export/config")]
    public async Task UpdateConfig(ExportConfig config)
    {
        using var db = new ImageContext();

        try
        {
            db.DownloadConfigs.Update(config);
            await db.SaveChangesAsync("SaveExportConfig");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error saving export config: {ex}");
            throw;
        }
    }

    [HttpPut("/api/export/config")]
    public async Task CreateConfig(ExportConfig config)
    {
        using var db = new ImageContext();

        try
        {
            if (db.DownloadConfigs.Any(x => x.Name.Equals(config.Name)))
                throw new ArgumentException($"Config {config.Name} already exists!");

            db.DownloadConfigs.Add(config);
            await db.SaveChangesAsync("SaveExportConfig");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error saving export config: {ex}");
            throw;
        }
    }
}

