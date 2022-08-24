using System;
using Damselfly.Core.DbModels;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.Constants;

namespace Damselfly.Core.ScopedServices;

public class ClientThemeService : IThemeService
{
    public ClientThemeService( RestClient client, IConfigService configService, ILogger<ClientThemeService> logger )
    {
        _configService = configService;
        httpClient = client;
        _logger = logger;
    }

    private readonly IConfigService _configService;
    private readonly RestClient httpClient;
    private ILogger<ClientThemeService> _logger;

    public event Action<ThemeConfig> OnChangeTheme;

    // WASM: Need to load user's theme here

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

    public async Task<List<ThemeConfig>> GetAllThemes()
    {
        try
        {
            return await httpClient.CustomGetFromJsonAsync<List<ThemeConfig>>($"/api/themes");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetThemes: {ex.Message}");
            return null;
        }
    }

    public async Task SetNewTheme(ThemeConfig newTheme)
    {
        _configService.Set(ConfigSettings.Theme, newTheme.Name);

        OnChangeTheme?.Invoke( newTheme );
    }
}

