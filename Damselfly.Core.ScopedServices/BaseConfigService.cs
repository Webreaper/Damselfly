using System;
using System.Collections.Generic;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices;

public abstract class BaseConfigService
{
    private readonly IDictionary<string, ConfigSetting> _cache = new Dictionary<string, ConfigSetting>(StringComparer.OrdinalIgnoreCase);

    public abstract void InitialiseCache();

    protected void ClearCache()
    {
        lock (_cache)
        {
            _cache.Clear();
        }
    }

    public virtual void SetSetting(string name, ConfigSetting value)
    {
        lock (_cache)
        {
            if (value == null)
                _cache.Remove(name);
            else
                _cache[name] = value;
        }
    }

    public ConfigSetting GetSetting(string name)
    {
        lock (_cache)
        {
            if (_cache.TryGetValue(name, out ConfigSetting value))
                return value;

            return null;
        }
    }

    public void Set(string name, string value)
    {
        SetSetting(name, new ConfigSetting { Name = name, Value = value } );
    }

    public string Get(string name, string defaultIfNotExists = null)
    {
        var existing = GetSetting(name);

        if (existing != null)
            return existing.Value;
        
        return defaultIfNotExists;
    }

    public EnumType Get<EnumType>(string name, EnumType defaultIfNotExists = default) where EnumType : struct
    {
        EnumType resultInputType = defaultIfNotExists;

        var setting = Get(name);

        if (!string.IsNullOrEmpty(setting))
        {
            if (!Enum.TryParse(setting, true, out resultInputType))
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
}

