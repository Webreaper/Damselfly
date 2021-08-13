using System;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.Models;
using Damselfly.Core.Interfaces;
using Damselfly.Core.DbModels;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// Service to store NVP configuration settings
    /// </summary>
    public class ConfigService : IConfigService
    {
        private readonly IDictionary<string, ConfigSetting> _cache = new Dictionary<string, ConfigSetting>(StringComparer.OrdinalIgnoreCase);

        public ConfigService()
        {
        }

        public void InitialiseCache( bool force = false, IDamselflyUser user = null)
        {
            if (_cache == null || force)
            {
                using var db = new ImageContext();
                List<ConfigSetting> settings;

                if (user != null)
                    settings = db.ConfigSettings.Where(x => x.UserId == user.Id || x.UserId == null).ToList();
                else
                    settings = db.ConfigSettings.ToList();

                foreach (var setting in db.ConfigSettings.Where( x => x.UserId == user.Id || x.UserId == null ) )
                    _cache[setting.Name] = setting;
            }
        }

        public void Set(string name, string value, IDamselflyUser user = null )
        {
            InitialiseCache();

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
                        existing.UserId = user?.Id;
                        db.ConfigSettings.Update(existing);
                    }
                }
                else
                {
                    if (!String.IsNullOrEmpty(value))
                    {
                        // Existing setting set to non-null - create in the DB and cache.
                        existing = new ConfigSetting { Name = name, Value = value, UserId = user?.Id };
                        _cache[name] = existing;
                        db.ConfigSettings.Add(existing);
                    }
                }

                db.SaveChanges("SaveConfig");
            }

            // Clear the cache and re-initialise it
            InitialiseCache(true, user);
        }

        public string Get(string name, string defaultIfNotExists = null, IDamselflyUser user = null )
        {
            InitialiseCache( false, user );

            if (_cache.TryGetValue(name, out ConfigSetting existing))
                return existing.Value;

            return defaultIfNotExists;
        }

        public EnumType Get<EnumType>(string name, EnumType defaultIfNotExists = default, IDamselflyUser user = null ) where EnumType : struct
        {
            EnumType resultInputType = defaultIfNotExists;

            string value = Get(name, null, user);

            if (!string.IsNullOrEmpty(value))
            {
                if (!Enum.TryParse(value, true, out resultInputType))
                    resultInputType = defaultIfNotExists;
            }

            return resultInputType;
        }

        public bool GetBool(string name, bool defaultIfNotExists = default, IDamselflyUser user = null)
        {
            bool result = defaultIfNotExists;

            string value = Get(name, null, user);

            if (!string.IsNullOrEmpty(value))
            {
                if (!bool.TryParse(value, out result))
                    result = defaultIfNotExists;
            }

            return result;
        }

        public int GetInt(string name, int defaultIfNotExists = default, IDamselflyUser user = null)
        {
            int result = defaultIfNotExists;

            string value = Get(name, null, user);

            if (!string.IsNullOrEmpty(value))
            {
                if (!int.TryParse(value, out result))
                    result = defaultIfNotExists;
            }

            return result;
        }
    }
}