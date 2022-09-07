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
    private readonly IUserService _userService;
    private readonly NotificationsService _notifications;
    private readonly RestClient httpClient;

    public ClientConfigService( RestClient restClient, NotificationsService notifications, IUserService userService, ILogger<IConfigService> logger ) : base( logger )
    {
        _notifications = notifications;
        _userService = userService;
        httpClient = restClient;

        _notifications.SubscribeToNotification( Constants.NotificationType.SystemSettingsChanged, SystemSettingsChanged );

        _userService.OnUserIdChanged += UserIdChanged;

        _ = InitialiseCache();
    }

    public void Dispose()
    {
        _userService.OnUserIdChanged -= UserIdChanged;
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

    protected override async Task<List<ConfigSetting>> GetAllSettings()
    {
        List<ConfigSetting> allSettings;
        try
        {
            if ( _userService.UserId.HasValue )
                allSettings = await httpClient.CustomGetFromJsonAsync<List<ConfigSetting>>( $"/api/config/user/{_userService.UserId}" );
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
