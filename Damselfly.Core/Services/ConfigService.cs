using System;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.Models;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// Service to store NVP configuration settings
    /// </summary>
    public class ConfigService
    {
        public static ConfigService Instance { get; private set; }
        private IDictionary<string, ConfigSetting> _cache;

        public ConfigService()
        {
            Instance = this;
        }

        public void InitialiseCache( bool force = false )
        {
            if (_cache == null || force)
            {
                using var db = new ImageContext();
                _cache = db.ConfigSettings.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);
            }
        }

        public void Set(string name, string value)
        {
            InitialiseCache();

            using var db = new ImageContext();

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

            // Clear the cache and re-initialise it
            InitialiseCache(true);
        }

        public string Get(string name, string defaultIfNotExists = null)
        {
            InitialiseCache();

            if (_cache.TryGetValue(name, out ConfigSetting existing))
                return existing.Value;

            return defaultIfNotExists;
        }

        public EnumType Get<EnumType>(string name, EnumType defaultIfNotExists = default) where EnumType : struct
        {
            EnumType resultInputType = defaultIfNotExists;

            string value = Get(name);

            if (!string.IsNullOrEmpty(value))
            {
                if (!Enum.TryParse(value, true, out resultInputType))
                    resultInputType = defaultIfNotExists;
            }

            return resultInputType;
        }

        public bool GetBool(string name, bool defaultIfNotExists = default)
        {
            bool result = defaultIfNotExists;

            string value = Get(name);

            if (!string.IsNullOrEmpty(value))
            {
                if (!bool.TryParse(value, out result))
                    result = defaultIfNotExists;
            }

            return result;
        }
    }
}