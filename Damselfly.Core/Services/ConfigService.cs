using System;
using System.Linq;
using Damselfly.Core.Models;
using Damselfly.Core.Interfaces;

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
        lock (_cache)
        {
            _cache.Clear();
            using var db = new ImageContext();

            var settings = db.ConfigSettings.Where(x => x.UserId == null || x.UserId == 0).ToList();

            foreach (var setting in settings)
            {
                _cache[setting.Name] = setting;
            }
        }
    }

    public override void Set(string name, string value )
    {
        using var db = new ImageContext();

        lock (_cache)
        {
            if (_cache.TryGetValue(name, out ConfigSetting existing))
            {
                if (String.IsNullOrEmpty(value))
                {
                    // Setting set to null - delete from the DB and cache
                    db.ConfigSettings.Remove(existing);
                    _cache.Remove(name);
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
                    _cache[name] = existing;
                    db.ConfigSettings.Add(existing);
                }
            }

            db.SaveChanges("SaveConfig");
        }
    }
}
