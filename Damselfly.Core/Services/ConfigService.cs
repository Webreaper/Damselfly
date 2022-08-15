using System;
using System.Linq;
using Damselfly.Core.Models;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Core.ScopedServices;

namespace Damselfly.Core.Services;

/// <summary>
/// Service to store NVP configuration settings
/// </summary>
public class ConfigService : BaseConfigService, IConfigService
{
    public ConfigService()
    {
        InitialiseCache();
    }

    public override void InitialiseCache()
    {
        ClearCache();
        using var db = new ImageContext();

        var settings = db.ConfigSettings.Where(x => x.UserId == null || x.UserId == 0).ToList();

        foreach (var setting in settings)
        {
           SetSetting(setting.Name,  setting );
        }
    }

    public void Set(string name, string value )
    {
        using var db = new ImageContext();

        var existing = GetSetting(name);

        if ( existing != null )
        {
            if (String.IsNullOrEmpty(value))
            {
                // Setting set to null - delete from the DB and cache
                db.ConfigSettings.Remove(existing);
                Set(name, null);
            }
            else
            {
                // Setting set to non-null - save in the DB and cache
                existing.Value = value;
                db.ConfigSettings.Update(existing);
            }
        }
        else
        {
            if (!String.IsNullOrEmpty(value))
            {
                // Existing setting set to non-null - create in the DB and cache.
                existing = new ConfigSetting { Name = name, Value = value };
                base.SetSetting(name, existing);
                db.ConfigSettings.Add(existing);
            }
        }

        db.SaveChanges("SaveConfig");
    }
}
