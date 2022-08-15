using System;
using Damselfly.Core.DbModels;
using System.Net.Http.Json;
using MudBlazor;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using System.Xml.Linq;

namespace Damselfly.Core.ScopedServices;

public class ClientThemeService : BaseClientService
{
    public ClientThemeService( HttpClient client, ILogger<ClientThemeService> logger ) : base( client )  { _logger = logger; }

    private ILogger<ClientThemeService> _logger;

    // WASM: TODO: 
    public event Action<ThemeConfig> OnChangeTheme;

    public async Task<ThemeConfig> GetTheme(string name)
    {
        var uri = $"/api/theme";
        if( !string.IsNullOrEmpty( name ))
            uri = $"/api/theme/{name}";

        try
        {
            return await httpClient.GetFromJsonAsync<ThemeConfig>(uri);
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
            return await httpClient.GetFromJsonAsync<ThemeConfig>($"/api/theme");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetTheme: {ex}");
            return null;
        }
    }
}

