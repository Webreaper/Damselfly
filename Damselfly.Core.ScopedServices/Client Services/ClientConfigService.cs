using System;
using System.Linq;
using System.Xml.Linq;
using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class ClientConfigService : BaseConfigService, IConfigService, ISystemSettingsService, IDisposable
{
    private RestClient httpClient;
    private readonly AuthenticationStateProvider _authProvider;

    public ClientConfigService(RestClient restClient, AuthenticationStateProvider authProvider, ILogger<IConfigService> logger) : base(logger)
    {
        httpClient = restClient;
        _authProvider = authProvider;

        _authProvider.AuthenticationStateChanged += AuthStateChanged;

        _ = InitialiseCache();
    }

    private async void AuthStateChanged(Task<AuthenticationState> task)
    {
        // User has changed. Clear the cache
        _ = InitialiseCache();
    }

    public override async Task<List<ConfigSetting>> GetAllSettings()
    {
        try
        {
            if (httpClient is null)
                throw new ArgumentException("Rest client is NULL!");

            return await httpClient.CustomGetFromJsonAsync<List<ConfigSetting>>($"/api/config");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception loading all settings: {ex.Message}");
            return new List<ConfigSetting>();
        }
    }

    public override bool SetSetting(string name, ConfigSetting setting)
    {
        if (!base.SetSetting(name, setting))
            return false;

        var payload = new ConfigSetRequest { Name = name, NewValue = setting.Value };
        _ = httpClient.CustomPutAsJsonAsync($"/api/config", payload);
        return true;
    }

    public override ConfigSetting GetSetting(string name)
    {
        var existing = base.GetSetting(name);

        if (existing == null)
        {
            // TODO: WASM
            // Can we just rely on the cache here? Or should we fix this?
            // existing = httpClient.CustomGetFromJsonAsync<ConfigSetting>($"/api/config/{name}");
        }

        return existing;
    }

    public virtual async Task<SystemConfigSettings> GetSystemSettings()
    {
        return await httpClient.CustomGetFromJsonAsync<SystemConfigSettings>($"/api/config/settings");
    }

    public virtual async Task SaveSystemSettings(SystemConfigSettings settings)
    {
        await httpClient.CustomPostAsJsonAsync<SystemConfigSettings>($"/api/config/settings", settings);
    }

    public void Dispose()
    {
        _authProvider.AuthenticationStateChanged -= AuthStateChanged;
    }
}
