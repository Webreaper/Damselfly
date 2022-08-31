using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class UserConfigService : BaseConfigService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUserService _userService;
    private int? _userId;

    public UserConfigService(IUserService userService, ILogger<IConfigService> logger) : base( logger )
    {
        _userService = userService;
        _userService.OnUserIdChanged += UserChanged;
        _userId = userService.UserId;

        _ = InitialiseCache();
    }

    private void UserChanged( int? userId )
    {
        _userId = userId;
        _ = InitialiseCache();
    }

    public void Dispose()
    {
        _userService.OnUserIdChanged -= UserChanged;
    }

    public override async Task<List<ConfigSetting>> GetAllSettings()
    {
        if (_userId != -1)
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetService<ImageContext>();

            var settings = await db.ConfigSettings.Where(x => x.UserId == _userId).ToListAsync();

            return settings;
        }

        return new List<ConfigSetting>();
    }

    /// <summary>
    /// Save the settings. Note that if there is no logged in
    /// user, we store the settings in the cache with an ID of
    /// zero, and we don't save to the DB (which would give a
    /// FK constraint exception. Means that settings will work
    /// but only for the current session.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public void Set(string name, string value)
    {
        // UserID of less than zero indicates "no user", so default global setting
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var existing = GetSetting(name);

        if (existing != null )
        {
            if (String.IsNullOrEmpty(value))
            {
                Set(name, null);

                if( _userId != -1 )
                {
                    // Setting set to null - delete from the DB and cache
                    db.ConfigSettings.Remove(existing);
                }
            }
            else
            {
                // Setting set to non-null - save in the DB and cache
                existing.Value = value;
                existing.UserId = _userId;

                SetSetting(name, existing );

                if (_userId != -1)
                    db.ConfigSettings.Update(existing);
            }
        }
        else
        {
            if (!String.IsNullOrEmpty(value))
            {
                // Existing setting set to non-null - create in the DB and cache.
                existing = new ConfigSetting { Name = name, Value = value, UserId = _userId };
                SetSetting( name, existing );

                if (_userId != -1)
                    db.ConfigSettings.Add(existing);
            }
        }

        if (_userId != -1)
            db.SaveChanges("SaveConfig");
    }
}
