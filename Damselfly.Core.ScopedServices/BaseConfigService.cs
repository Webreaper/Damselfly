using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public abstract class BaseConfigService
{
    private readonly IDictionary<string, ConfigSetting> _cache = new ConcurrentDictionary<string, ConfigSetting>(StringComparer.OrdinalIgnoreCase);
    protected readonly ILogger<IConfigService> _logger;

    protected BaseConfigService(ILogger<IConfigService> logger)
    {
        _logger = logger;
    }

    public abstract Task<List<ConfigSetting>> GetAllSettings();

    protected async Task InitialiseCache()
    {
        _logger.LogInformation("Preloading config cache with all settings...");

        var allSettings = await GetAllSettings();

        _cache.Clear();

        if (allSettings.Any())
        {
            allSettings.ForEach(x => _cache[x.Name] = x);
            _logger.LogInformation($"Loaded {allSettings.Count()} settings into config cache.");
        }
    }

    protected void ClearCache()
    {
        _cache.Clear();
    }

    public virtual bool SetSetting(string name, ConfigSetting value)
    {
        if( _cache.TryGetValue( name, out var existingValue ))
        {
            if (existingValue.Equals(value))
                return false;
        }

        if (value == null)
            _cache.Remove(name);
        else
            _cache[name] = value;

        return true;
    }

    protected virtual ConfigSetting GetSetting(string name)
    {
        if (_cache.TryGetValue(name, out var value))
            return value;

        return null;
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

