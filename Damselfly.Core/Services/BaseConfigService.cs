using System;
using System.Collections.Generic;
using Damselfly.Core.Models;

namespace Damselfly.Core.Services;

public abstract class BaseConfigService
{
    protected readonly IDictionary<string, ConfigSetting> _cache = new Dictionary<string, ConfigSetting>(StringComparer.OrdinalIgnoreCase);

    public abstract void Set(string name, string value);
    public abstract void InitialiseCache();

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
}

