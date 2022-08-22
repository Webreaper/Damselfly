using System;
using System.Linq;
using System.Xml.Linq;
using Damselfly.Core.DbModels;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

// TODO: Write values to the back-end service
public class ClientConfigService : BaseConfigService, IConfigService
{
    private RestClient httpClient;

    public ClientConfigService( RestClient restClient, ILogger<IConfigService> logger ) : base(logger)
    {
        httpClient = restClient;

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

    public override bool SetSetting(string name, ConfigSetting value)
    {
        if (!base.SetSetting(name, value))
            return false;

        _ = httpClient.CustomPutAsJsonAsync($"/api/config/{name}", value);
        return true;
    }

    public virtual ConfigSetting GetSetting(string name)
    {
        var existing = base.GetSetting(name);

        if( existing == null )
        {
            existing = httpClient.CustomGetFromJsonAsync<ConfigSetting>($"/api/config/{name}").Result;
        }

        return existing;
    }
}
