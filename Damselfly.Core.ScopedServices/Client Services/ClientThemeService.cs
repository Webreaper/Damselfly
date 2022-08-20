using System;
using Damselfly.Core.DbModels;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.ScopedServices.ClientServices;

namespace Damselfly.Core.ScopedServices;

public class ClientThemeService : IThemeService
{
    public ClientThemeService( RestClient client, ILogger<ClientThemeService> logger )
    {
        httpClient = client;
        _logger = logger;
    }

    private readonly RestClient httpClient;
    private ILogger<ClientThemeService> _logger;

    // WASM: TODO: 
    public event Action<ThemeConfig> OnChangeTheme;

    public async Task<ThemeConfig> GetThemeConfig(string name)
    {
        var uri = $"/api/theme";

        if( !string.IsNullOrEmpty( name ))
            uri = $"/api/theme/{name}";

        try
        {
            return await httpClient.CustomGetFromJsonAsync<ThemeConfig>(uri);
        }
        catch( Exception ex )
        {
            _logger.LogError($"Error in GetTheme: {ex}");
            return await GetDefaultTheme();
        }
    }


    public async Task<ThemeConfig> GetDefaultTheme()
    {
        try
        {
            return await httpClient.CustomGetFromJsonAsync<ThemeConfig>($"/api/theme");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetTheme: {ex.Message}");
            return null;
        }
    }
}

