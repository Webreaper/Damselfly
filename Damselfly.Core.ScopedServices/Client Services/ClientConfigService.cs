using System;
using System.Collections.Concurrent;
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
    private readonly NotificationsService _notifications;
    private readonly RestClient httpClient;
    private readonly AuthenticationStateProvider _authProvider;
    private int? UserId;

    public ClientConfigService( RestClient restClient, AuthenticationStateProvider authProvider, NotificationsService notifications, ILogger<IConfigService> logger ) : base( logger )
    {
        _authProvider = authProvider;
        _notifications = notifications;
        httpClient = restClient;

        _notifications.SubscribeToNotification( Constants.NotificationType.SystemSettingsChanged, SystemSettingsChanged );

        _authProvider.AuthenticationStateChanged += AuthStateChanged;

        _ = InitialiseCache();
    }

    private async void AuthStateChanged( Task<AuthenticationState> authStateTask )
    {
        AuthenticationState authState = await authStateTask;
        this.UserId = authState.GetUserIdFromPrincipal();
        await InitialiseCache();
    }

    public void Dispose()
    {
        _authProvider.AuthenticationStateChanged -= AuthStateChanged;
    }

    private void SystemSettingsChanged()
    {
        // Another user changed the system settings - so refresh
        _ = InitialiseCache();
    }

    private void UserIdChanged( int? newUserId )
    {
        // User has changed. Clear the cache
        _ = InitialiseCache();
    }

    protected override async Task PersistSetting( ConfigSetRequest saveRequest )
    {
        // Save remotely
        await httpClient.CustomPutAsJsonAsync( $"/api/config", saveRequest );
    }

    protected override async Task<List<ConfigSetting>> LoadAllSettings()
    {
        List<ConfigSetting> allSettings;
        try
        {
            if ( UserId.HasValue )
                allSettings = await httpClient.CustomGetFromJsonAsync<List<ConfigSetting>>( $"/api/config/user/{UserId}" );
            else
                allSettings = await httpClient.CustomGetFromJsonAsync<List<ConfigSetting>>( $"/api/config" );
        }
        catch ( Exception ex )
        {
            _logger.LogError( $"Exception loading all settings: {ex.Message}" );
            allSettings = new List<ConfigSetting>();
        }

        return allSettings;
    }

    public async Task<SystemConfigSettings> GetSystemSettings ()
    {
        return await httpClient.CustomGetFromJsonAsync<SystemConfigSettings>( $"/api/config/settings" );
    }

    public async Task SaveSystemSettings ( SystemConfigSettings settings )
    {
        await httpClient.CustomPostAsJsonAsync<SystemConfigSettings>( $"/api/config/settings", settings );
    }
}
