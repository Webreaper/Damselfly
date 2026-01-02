using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Database;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stopwatch = Damselfly.Shared.Utils.Stopwatch;

namespace Damselfly.Core.Services;

/// <summary>
///     Service to store NVP configuration settings
/// </summary>
public class ConfigService : BaseConfigService, IConfigService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ConfigService(IServiceScopeFactory scopeFactory, ILogger<IConfigService> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;

        _ = InitialiseCache();
    }

    protected override async Task<List<ConfigSetting>> LoadAllSettings()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = ImageContext.GetImageContext( scope );

        // Get all the settings that are either global, or match our user.
        var settings = await db.ConfigSettings
            .Where(x => x.UserId == null || x.UserId == 0)
            .ToListAsync();

        return settings;
    }
    
    // Used by the controller
    public async Task<List<ConfigSetting>> GetAllSettingsForUser(int? userId)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = ImageContext.GetImageContext( scope );

        if( userId != null && userId > 0 )
        {
            // Get all the settings that are either global, or match our user.
            var userSettings = db.ConfigSettings.Where(x => x.UserId == userId);
            var globalSettings = db.ConfigSettings.Where(x => x.UserId == 0 || (x.UserId == null &&
                !userSettings.Select(x => x.Name).Contains(x.Name)));

            // Combine them together.
            return await userSettings.Concat(globalSettings)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        // No user, so just return the global settings.
        return await db.ConfigSettings
            .Where(x => x.UserId == 0 || x.UserId == null)
            .OrderBy( x => x.Name )
            .ToListAsync();
    }

    protected override async Task PersistSettings(IDictionary<string, ConfigSetting> allSettings)
    {
        var changes = false;
        
        var watch = new Stopwatch("PersistSettings");
        
        using var scope = _scopeFactory.CreateScope();
        await using var db = ImageContext.GetImageContext( scope );

        var existingSettings = await db.ConfigSettings.ToListAsync();
        var allExisting = existingSettings
                                .DistinctBy(x => x.CacheKey)
                                .ToDictionary(x => x.CacheKey, x => x);

        foreach( var setting in allSettings )
        {
            if( allExisting.TryGetValue(setting.Key, out var existingSetting) )
            {
                if( existingSetting.Value == setting.Value.Value )
                    continue; // Nothing to do

                changes = true;
                if( setting.Value.Value == null )
                {
                    // Remove
                    db.ConfigSettings.Remove(existingSetting);
                }
                else
                {
                    // Update - this should mark the item as modified.
                    existingSetting.Value = setting.Value.Value;
                    db.ConfigSettings.Update(existingSetting);
                }
            }
            else
            {
                changes = true;
                // It didn't exist previously - Save the new one
                var newEntry = new ConfigSetting { Name = setting.Key, Value = setting.Value.Value, UserId = setting.Value.UserId };
                db.ConfigSettings.Add(newEntry);            
            }
        }

        if( changes )
        {
            watch.Stop();
            _logger.LogInformation("Saving config settings took {T}", watch.ElapsedTime);
            await db.SaveChangesAsync("SaveConfig");
        }
        else
            _logger.LogInformation("No config changes to save");
    }
}