using System;
using System.Linq;
using Damselfly.Core.Models;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Core.ScopedServices;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Damselfly.Core.ScopedServices.Interfaces;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.DbModels.Models;
using Damselfly.ML.Face.Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Damselfly.Core.DbModels.Models.APIModels;

namespace Damselfly.Core.Services;

/// <summary>
/// Service to store NVP configuration settings
/// </summary>
public class ConfigService : BaseConfigService, IConfigService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ConfigService( IServiceScopeFactory scopeFactory, ILogger<IConfigService> logger ) : base( logger )
    {
        _scopeFactory = scopeFactory;

        _ = InitialiseCache();
    }

    protected override async Task<List<ConfigSetting>> LoadAllSettings()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        // Get all the settings that are either global, or match our user.
        var settings = await db.ConfigSettings
                                    .Where( x => x.UserId == null || x.UserId == 0 )
                                    .ToListAsync();

        return settings;
    }

    // Used By the Controller
    public async Task SetSetting( ConfigSetRequest setRequest )
    {
        await PersistSetting( setRequest );
    }

    // Used by the controller
    public async Task<List<ConfigSetting>> GetAllSettingsForUser( int? userId )
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        if ( userId != null && userId > 0 )
        {
            // Get all the settings that are either global, or match our user.
            var userSettings = db.ConfigSettings.Where( x => x.UserId == userId );
            var globalSettings = db.ConfigSettings.Where( x => x.UserId == 0 || x.UserId == null &&
                                                    !userSettings.Select( x => x.Name ).Contains( x.Name ) );

            // Combine them together.
            return await userSettings.Concat( globalSettings )
                                                .ToListAsync();
        }
        else
        {
            // No user, so just return the global settings.
            return await db.ConfigSettings
                                   .Where( x => x.UserId == 0 || x.UserId == null )
                                   .ToListAsync();
        }
    }

    protected override async Task PersistSetting( ConfigSetRequest setRequest )
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var existing = await db.ConfigSettings
                               .Where( x => x.Name == setRequest.Name && x.UserId == setRequest.UserId )
                               .FirstOrDefaultAsync();

        if ( String.IsNullOrEmpty( setRequest.NewValue ) )
        {
            // Setting set to null - delete from the DB and cache
            if ( existing != null )
                db.ConfigSettings.Remove( existing );
        }
        else
        {
            // Set the value - either update the existing or create a new one
            if ( existing != null )
            {
                // Setting set to non-null - save in the DB and cache
                existing.Value = setRequest.NewValue;
                db.ConfigSettings.Update( existing );
            }
            else
            {
                // Existing setting set to non-null - create in the DB and cache.
                var newEntry = new ConfigSetting { Name = setRequest.Name, Value = setRequest.NewValue, UserId = setRequest.UserId };
                db.ConfigSettings.Add( newEntry );
            }
        }

        db.SaveChanges("SaveConfig");
    }
}
