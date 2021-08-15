using System;
using System.Linq;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.Services
{
    public class UserConfigService : BaseConfigService
    {
        private UserService _userService;
        private AppIdentityUser _user;

        public UserConfigService(UserService userService)
        {
            _userService = userService;
            _userService.OnChange += UserChanged;
            _user = userService.User;

            InitialiseCache();
        }

        private void UserChanged( AppIdentityUser user )
        {
            _user = user;
            InitialiseCache();
        }

        public override void InitialiseCache()
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

        public override void Set(string name, string value)
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
