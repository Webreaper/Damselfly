using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class ClientThemeService : IThemeService, IDisposable
{
    private readonly IUserConfigService _configService;
    private readonly ILogger<ClientThemeService> _logger;
    private readonly RestClient httpClient;

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

    public event Action<ThemeConfig> OnChangeTheme;

    public async Task<ThemeConfig> GetThemeConfig(string name)
    {
        var uri = "/api/theme";

        if ( !string.IsNullOrEmpty(name) )
            uri = $"/api/theme/{name}";

        try
        {
            return await httpClient.CustomGetFromJsonAsync<ThemeConfig>(uri);
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Error in GetTheme: {ex}");
            return await GetDefaultTheme();
        }
    }

    public async Task<ThemeConfig> GetDefaultTheme()
    {
        try
        {
            return await httpClient.CustomGetFromJsonAsync<ThemeConfig>("/api/theme");
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Error in GetTheme: {ex.Message}");
            return null;
        }
    }

    public async Task<List<ThemeConfig>> GetAllThemes()
    {
        try
        {
            return await httpClient.CustomGetFromJsonAsync<List<ThemeConfig>>("/api/themes");
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Error in GetThemes: {ex.Message}");
            return null;
        }
    }

    public async Task ApplyTheme(string themeName)
    {
        var themeConfig = await GetThemeConfig(themeName);

        if ( themeConfig != null )
            await ApplyTheme(themeConfig);
    }

    public Task ApplyTheme(ThemeConfig newTheme)
    {
        OnChangeTheme?.Invoke(newTheme);
        return Task.CompletedTask;
    }

    private void SettingsLoaded(ICollection<ConfigSetting> newSettings)
    {
        var themeSetting = newSettings.FirstOrDefault(x => x.Name == ConfigSettings.Theme);

        if ( themeSetting != null )
            _ = ApplyTheme(themeSetting.Value);
    }

    public async Task SetTheme(string themeName)
    {
        await _configService.SetForUser(ConfigSettings.Theme, themeName);

        await ApplyTheme(themeName);
    }
}