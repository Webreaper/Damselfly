using System;
using Damselfly.Core.DbModels;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.Constants;
using Microsoft.AspNetCore.Components.Authorization;

namespace Damselfly.Core.ScopedServices;

public class ClientThemeService : IThemeService, IDisposable
{
    public ClientThemeService(RestClient client, IUserConfigService configService, ILogger<ClientThemeService> logger)
    {
        _configService = configService;
        httpClient = client;
        _logger = logger;

        _configService.OnSettingsLoaded += SettingsLoaded;
    }

    public void Dispose()
    {
        _configService.OnSettingsLoaded -= SettingsLoaded;
    }

    private readonly IUserConfigService _configService;
    private readonly RestClient httpClient;
    private readonly ILogger<ClientThemeService> _logger;
    private readonly AuthenticationStateProvider _authProvider;

    public event Action<ThemeConfig> OnChangeTheme;

    private void SettingsLoaded()
    {
        var themeName = _configService.Get( ConfigSettings.Theme, "Green" );
        _ = SetNewTheme( themeName );
    }

    public async Task<ThemeConfig> GetThemeConfig(string name)
    {
        var uri = $"/api/theme";

        if (!string.IsNullOrEmpty(name))
            uri = $"/api/theme/{name}";

        try
        {
            return await httpClient.CustomGetFromJsonAsync<ThemeConfig>(uri);
        }
        catch (Exception ex)
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

    public async Task SetNewTheme( string themeName )
    {
        var themeConfig = await GetThemeConfig( themeName );

        if( themeConfig != null )
            await SetNewTheme( themeConfig );
    }

    public async Task SetNewTheme(ThemeConfig newTheme)
    {
        _configService.SetForUser(ConfigSettings.Theme, newTheme.Name);

        OnChangeTheme?.Invoke(newTheme);
    }
}

