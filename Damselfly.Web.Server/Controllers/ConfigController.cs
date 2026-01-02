using Damselfly.Core.DbModels.Models;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/config")]
public class ConfigController : ControllerBase
{
    private readonly ConfigService _configService;
    private readonly ILogger<ConfigController> _logger;
    private readonly ISystemSettingsService _settingsService;

    public ConfigController(ConfigService configService, SystemSettingsService settingsService,
        ILogger<ConfigController> logger)
    {
        _configService = configService;
        _settingsService = settingsService;
        _logger = logger;
    }

    [HttpPut("/api/config")]
    public async Task Set(ConfigSetting setting)
    {
        await _configService.SetSetting(setting);
    }

    [HttpGet("/api/config/user/{userId}")]
    public async Task<List<ConfigSetting>> GetAllSettingsForUser(int userId)
    {
        _logger.LogInformation($"Loading all config value for user {userId}...");
        var settings = new List<ConfigSetting>();

        var allValues = await _configService.GetAllSettingsForUser(userId);
        if ( allValues != null )
            settings.AddRange(allValues);

        return settings;
    }

    [HttpGet("/api/config")]
    public async Task<List<ConfigSetting>> GetAllSettings()
    {
        _logger.LogInformation("Loading all config values...");
        var settings = new List<ConfigSetting>();

        var allValues = await _configService.GetAllSettingsForUser(null);
        if ( allValues != null )
            settings.AddRange(allValues);

        return settings;
    }

    [HttpGet("/api/config/{name}")]
    public ConfigSetting Get(string name)
    {
        var value = _configService.Get(name);
        return new ConfigSetting { Name = name, Value = value };
    }

    [HttpPost("/api/config/settings")]
    public async Task SetSysteemSettings(SystemConfigSettings settings)
    {
        await _settingsService.SaveSystemSettings(settings);
    }

    [HttpGet("/api/config/settings")]
    public async Task<SystemConfigSettings> GetSystemSettings()
    {
        return await _settingsService.GetSystemSettings();
    }
}