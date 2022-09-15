using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class UserConfigService : BaseConfigService, IDisposable
{
    private readonly IUserService _userService;
    private readonly IServiceScopeFactory _scopeFactory;

    public UserConfigService(IUserService userService, IServiceScopeFactory scopeFactory, ILogger<IConfigService> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _userService = userService;
        _userService.OnUserIdChanged += UserChanged;

        _ = InitialiseCache();
    }

    private void UserChanged(int? userId)
    {
        _ = InitialiseCache();
    }

    public void Dispose()
    {
        _userService.OnUserIdChanged -= UserChanged;
    }

    protected override async Task<List<ConfigSetting>> LoadAllSettings()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var settings = await db.ConfigSettings.Where(x => x.UserId == _userService.UserId).ToListAsync();

        return settings;
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

        db.SaveChanges( "SaveConfig" );
    }
}
