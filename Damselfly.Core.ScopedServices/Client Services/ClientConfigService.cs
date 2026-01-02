using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class ClientConfigService : BaseConfigService, IUserConfigService, ISystemSettingsService, IDisposable
{
    private readonly AuthenticationStateProvider _authProvider;
    private readonly NotificationsService _notifications;
    private readonly RestClient httpClient;
    private int? _userId;

    public ClientConfigService(RestClient restClient, AuthenticationStateProvider authProvider,
        NotificationsService notifications,
        ILogger<IConfigService> logger) : base(logger)
    {
        _authProvider = authProvider;
        _notifications = notifications;
        httpClient = restClient;

        _notifications.SubscribeToNotification(NotificationType.SystemSettingsChanged, SystemSettingsChanged);

        _authProvider.AuthenticationStateChanged += AuthStateChanged;
    }

    public void Dispose()
    {
        _authProvider.AuthenticationStateChanged -= AuthStateChanged;
    }

    public async Task<SystemConfigSettings> GetSystemSettings()
    {
        return await httpClient.CustomGetFromJsonAsync<SystemConfigSettings>("/api/config/settings");
    }

    public async Task SaveSystemSettings(SystemConfigSettings settings)
    {
        await httpClient.CustomPostAsJsonAsync("/api/config/settings", settings);
    }

    public async Task SetForUser(string name, string value)
    {
        var newSetting = new ConfigSetting { Name = name, Value = value, UserId = _userId };
        await SetSetting(newSetting);
    }

    public override async Task SetSetting(ConfigSetting setting)
    {
        var req = new ConfigSetRequest
        {
            Name = setting.Name,
            NewValue = setting.Value,
            UserId = setting.UserId
        };

        // Ensure we update the client cache
        await base.SetSetting(setting);
        
        // Save remotely
        await httpClient.CustomPutAsJsonAsync("/api/config", req);
    }
    
    private async void AuthStateChanged(Task<AuthenticationState> authStateTask)
    {
        var authState = await authStateTask;
        _userId = authState.GetUserIdFromPrincipal();
        await base.InitialiseCache();
    }

    public override async Task InitialiseCache()
    {
        var state = await _authProvider.GetAuthenticationStateAsync();
        _userId = state.GetUserIdFromPrincipal();
        await base.InitialiseCache();
    }

    private void SystemSettingsChanged()
    {
        // Another user changed the system settings - so refresh
        _ = InitialiseCache();
    }
    
    protected override async Task<List<ConfigSetting>> LoadAllSettings()
    {
        List<ConfigSetting>? allSettings;
        try
        {
            var url = "/api/config";
            if ( _userId.HasValue )
                url = $"/api/config/user/{_userId}";
            
            allSettings = await httpClient.CustomGetFromJsonAsync<List<ConfigSetting>>(url);
        }
        catch ( Exception ex )
        {
            _logger.LogError(ex, $"Exception loading all settings");
            allSettings = new List<ConfigSetting>();
        }

        return allSettings;
    }
}