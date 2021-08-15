using System;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.DbModels;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;

namespace Damselfly.Core.Services
{
    public class UserConfigService
    {
        private ConfigService _configService;
        private UserService _userService;
        private AppIdentityUser _user;
        private readonly IDictionary<string, ConfigSetting> _cache = new Dictionary<string, ConfigSetting>(StringComparer.OrdinalIgnoreCase);

        public UserConfigService(ConfigService configService, UserService userService)
        {
            _configService = configService;
            _userService = userService;
            _userService.OnChange += UserChanged;
            _user = userService.User;

            if (_user != null)
                InitialiseCache();
        }

        private void UserChanged( AppIdentityUser user )
        {
            _user = user;
            InitialiseCache();
        }

        public void InitialiseCache()
        {
            lock (_cache)
            {
                _cache.Clear();

                if (_user != null)
                {
                    using var db = new ImageContext();

                    var settings = db.ConfigSettings.Where(x => x.UserId == _user.Id).ToList();

                    foreach (var setting in settings)
                    {
                        _cache[setting.Name] = setting;
                    }
                }
            }
        }


        public string Get(string name, string defaultIfNotExists = null)
        {
            if (_cache.TryGetValue(name, out ConfigSetting existing))
                return existing.Value;

            return defaultIfNotExists;
        }

        public EnumType Get<EnumType>(string name, EnumType defaultIfNotExists = default) where EnumType : struct
        {
            EnumType resultInputType = defaultIfNotExists;

            string value = Get(name, null);

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

            string value = Get(name, null);

            if (!string.IsNullOrEmpty(value))
            {
                if (!bool.TryParse(value, out result))
                    result = defaultIfNotExists;
            }

            return result;
        }

        public int GetInt(string name, int defaultIfNotExists = default)
        {
            int result = defaultIfNotExists;

            string value = Get(name, null);

            if (!string.IsNullOrEmpty(value))
            {
                if (!int.TryParse(value, out result))
                    result = defaultIfNotExists;
            }

            return result;
        }

        public void Set(string name, string value)
        {
            if (_user == null)
                return;

            lock (_cache)
            {
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
                        existing.UserId = _user.Id;
                        db.ConfigSettings.Update(existing);
                    }
                }
                else
                {
                    if (!String.IsNullOrEmpty(value))
                    {
                        // Existing setting set to non-null - create in the DB and cache.
                        existing = new ConfigSetting { Name = name, Value = value, UserId = _user.Id };
                        _cache[name] = existing;
                        db.ConfigSettings.Add(existing);
                    }
                }

                db.SaveChanges("SaveConfig");
            }
        }
    }
}
