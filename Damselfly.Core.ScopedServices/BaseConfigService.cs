using System.Collections.Concurrent;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public abstract class BaseConfigService
{
    private readonly IDictionary<string, ConfigSetting> _cache =
        new ConcurrentDictionary<string, ConfigSetting>(StringComparer.OrdinalIgnoreCase);

    protected readonly ILogger<IConfigService> _logger;

    public BaseConfigService(ILogger<IConfigService> logger)
    {
        _logger = logger;
    }

    public event Action<ICollection<ConfigSetting>> OnSettingsLoaded;

    protected abstract Task<List<ConfigSetting>> LoadAllSettings();

    public virtual async Task InitialiseCache()
    {
        _logger.LogInformation("Preloading config cache with all settings...");

        var allSettings = await LoadAllSettings();

        _cache.Clear();

        if ( allSettings.Any() )
        {
            allSettings.ForEach(x => _cache[x.CacheKey] = x);
            _logger.LogInformation("Loaded {C} settings into config cache", allSettings.Count);
        }

        OnSettingsLoaded?.Invoke(allSettings);
    }

    // Used by the controller
    public async Task<List<ConfigSetting>> GetAllSettingsForUser(int? userId)
    {
        // Get all the settings that are either global, or match our user.
        var settings = _cache.Values
                                                      .Where(x => x.UserId == userId)
                                                      .ToDictionary(x => x.Name);
        
        var globalSettings = _cache.Values.Where(x => x.UserId == 0 || x.UserId == null).ToList();
        
        foreach( var setting in globalSettings)
            settings.TryAdd(setting.Name, setting);

        // Combine them together.
        return settings.Values
            .OrderBy(x => x.Name)
            .ToList();
    }

    private void ClearCache()
    {
        _cache.Clear();
    }

    public virtual async Task SetSetting(ConfigSetting setting)
    {
        if( setting == null )
            throw new ArgumentException( $"Invalid setting passed to SetSetting" );

        if ( _cache.TryGetValue(setting.CacheKey, out var existingValue) )
        {
            // Existing cache value is the same, so do nothing
            if ( !string.IsNullOrEmpty(existingValue.Value) && existingValue.Value.Equals(setting.Value) )
                return;
        }

        // Update the cache
        if ( setting.Value == null )
            _cache.Remove(setting.CacheKey);
        else
            _cache[setting.CacheKey] = setting;
    }

    private ConfigSetting? GetSetting(string name)
    {
        var setting = new ConfigSetting { Name = name };
        if ( _cache.TryGetValue(setting.CacheKey, out var value) )
            return value;

        return null;
    }

    public async Task Set(string name, string value)
    {
        await SetSetting(new ConfigSetting { Name = name, Value = value });
    }

    public string Get(string name, string? defaultIfNotExists = null)
    {
        var existing = GetSetting(name);

        if ( existing != null )
            return existing.Value;

        return defaultIfNotExists;
    }

    public EnumType Get<EnumType>(string name, EnumType defaultIfNotExists = default) where EnumType : struct
    {
        var resultInputType = defaultIfNotExists;

        var setting = Get(name);

        if ( !string.IsNullOrEmpty(setting) )
            if ( !Enum.TryParse(setting, true, out resultInputType) )
                resultInputType = defaultIfNotExists;
        return resultInputType;
    }

    public bool GetBool(string name, bool defaultIfNotExists = default)
    {
        var result = defaultIfNotExists;

        var value = Get(name);

        if ( !string.IsNullOrEmpty(value) )
            if ( !bool.TryParse(value, out result) )
                result = defaultIfNotExists;

        return result;
    }

    public int GetInt(string name, int defaultIfNotExists = default)
    {
        var result = defaultIfNotExists;

        var value = Get(name);

        if ( !string.IsNullOrEmpty(value) )
            if ( !int.TryParse(value, out result) )
                result = defaultIfNotExists;

        return result;
    }

    protected virtual Task PersistSettings(IDictionary<string, ConfigSetting> allSettings)
    {
        throw new NotImplementedException();
    }

    public async Task SaveSettingsToDb()
    {
        await PersistSettings(_cache);
    }

}