using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
using System.Net.Http;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Damselfly.Core.ScopedServices.ClientServices;

namespace Damselfly.Core.ScopedServices;

public abstract class BaseConfigService
{
    private readonly IDictionary<string, ConfigSetting> _cache = new ConcurrentDictionary<string, ConfigSetting>( StringComparer.OrdinalIgnoreCase );
    protected readonly ILogger<IConfigService> _logger;

    public BaseConfigService( ILogger<IConfigService> logger )
    {
        _logger = logger;
    }

    protected abstract Task<List<ConfigSetting>> GetAllSettings();
    protected abstract Task PersistSetting( ConfigSetRequest setRequest );

    protected async Task InitialiseCache()
    {
        _logger.LogInformation( "Preloading config cache with all settings..." );

        var allSettings = await GetAllSettings();

        _cache.Clear();

        if ( allSettings.Any() )
        {
            allSettings.ForEach( x => _cache[x.Name] = x );
            _logger.LogInformation( $"Loaded {allSettings.Count()} settings into config cache." );
        }
    }

    private void ClearCache()
    {
        _cache.Clear();
    }

    private bool SetSetting( string name, ConfigSetting setting )
    {
        if ( _cache.TryGetValue( name, out var existingValue ) )
        {
            // Existing cache value is the same, so do nothing
            if ( existingValue.Equals( setting.Value ) )
                return false;
        }

        // Update the cache
        if ( setting.Value == null )
            _cache.Remove( name );
        else
            _cache[name] = setting;

        var saveReq = new ConfigSetRequest { Name = setting.Name, NewValue = setting.Value, UserId = setting.UserId };
        _ = PersistSetting( saveReq );

        return true;
    }

    private ConfigSetting GetSetting( string name )
    {
        if ( _cache.TryGetValue( name, out var value ) )
            return value;

        return null;
    }

    public void Set( string name, string value )
    {
        SetSetting( name, new ConfigSetting { Name = name, Value = value } );
    }

    public void SetForUser( string name, string value, int? userId )
    {
        SetSetting( name, new ConfigSetting { Name = name, Value = value, UserId = userId } );
    }

    public string Get( string name, string defaultIfNotExists = null )
    {
        var existing = GetSetting( name );

        if ( existing != null )
            return existing.Value;

        return defaultIfNotExists;
    }

    public EnumType Get<EnumType>( string name, EnumType defaultIfNotExists = default ) where EnumType : struct
    {
        EnumType resultInputType = defaultIfNotExists;

        var setting = Get( name );

        if ( !string.IsNullOrEmpty( setting ) )
        {
            if ( !Enum.TryParse( setting, true, out resultInputType ) )
                resultInputType = defaultIfNotExists;
        }
        return resultInputType;
    }

    public bool GetBool( string name, bool defaultIfNotExists = default )
    {
        bool result = defaultIfNotExists;

        string value = Get( name, null );

        if ( !string.IsNullOrEmpty( value ) )
        {
            if ( !bool.TryParse( value, out result ) )
                result = defaultIfNotExists;
        }

        return result;
    }

    public int GetInt( string name, int defaultIfNotExists = default )
    {
        int result = defaultIfNotExists;

        string value = Get( name, null );

        if ( !string.IsNullOrEmpty( value ) )
        {
            if ( !int.TryParse( value, out result ) )
                result = defaultIfNotExists;
        }

        return result;
    }
}

