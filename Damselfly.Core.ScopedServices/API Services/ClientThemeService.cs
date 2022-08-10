using System;
using Damselfly.Core.DbModels;
using System.Net.Http.Json;
using MudBlazor;

namespace Damselfly.Core.ScopedServices;

public class ClientThemeService : BaseClientService
{
    public ClientThemeService( HttpClient client ) : base( client )  {  }

    // WASM: TODO: 
    public event Action<ThemeConfig> OnChangeTheme;

    public async Task<ThemeConfig> GetTheme(string name)
    {
        return await httpClient.GetFromJsonAsync<ThemeConfig>($"/api/theme/{name}");
    }

    public async Task<ThemeConfig> GetTheme()
    {
        return await httpClient.GetFromJsonAsync<ThemeConfig>($"/api/theme");
    }
}

