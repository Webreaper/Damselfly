using System;
using System.Linq;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.Services;

namespace Damselfly.Core.ScopedServices;

public class UserConfigService : BaseConfigService, IDisposable
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

    public void Dispose()
    {
        _userService.OnChange -= UserChanged;
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

    /// <summary>
    /// Save the settings. Note that if there is no logged in
    /// user, we store the settings in the cache with an ID of
    /// zero, and we don't save to the DB (which would give a
    /// FK constraint exception. Means that settings will work
    /// but only for the current session.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public override void Set(string name, string value)
    {
        // UserID of zero indicates "no user", so default global setting
        int userId = _user != null ? _user.Id : 0;

        lock (_cache)
        {
            using var db = new ImageContext();

            if (_cache.TryGetValue(name, out ConfigSetting existing))
            {
                if (String.IsNullOrEmpty(value))
                {
                    _cache.Remove(name);

                    if (_user != null)
                    {
                        // Setting set to null - delete from the DB and cache
                        db.ConfigSettings.Remove(existing);
                    }
                }
                else
                {
                    // Setting set to non-null - save in the DB and cache
                    existing.Value = value;
                    existing.UserId = userId;

                    _cache[name] = existing;

                    if( _user != null )
                        db.ConfigSettings.Update(existing);
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(value))
                {
                    // Existing setting set to non-null - create in the DB and cache.
                    existing = new ConfigSetting { Name = name, Value = value, UserId = userId };
                    _cache[name] = existing;

                    if( _user != null )
                        db.ConfigSettings.Add(existing);
                }
            }

            if( _user != null )
                db.SaveChanges("SaveConfig");
        }
    }
}
