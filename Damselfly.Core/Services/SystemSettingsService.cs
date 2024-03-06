using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.Services;

/// <summary>
///     Service to store NVP configuration settings
/// </summary>
public class SystemSettingsService : ISystemSettingsService
{
    private readonly IConfigService _configService;
    private readonly ILogger<SystemSettingsService> _logger;
    private readonly ServerNotifierService _notifier;
    private readonly WorkService _workService;
    private readonly WordpressService _wpService;
    private readonly ThumbnailService _thumbService;

    public SystemSettingsService(ILogger<SystemSettingsService> logger,
        WorkService workService,
        WordpressService wpService,
        ThumbnailService thumbService,
        IConfigService configService,
        ServerNotifierService notifier)
    {
        _notifier = notifier;
        _workService = workService;
        _wpService = wpService;
        _configService = configService;
        _thumbService = thumbService;
        _logger = logger;
    }

    public Task<SystemConfigSettings> GetSystemSettings()
    {
        var settings = new SystemConfigSettings();
        settings.Load(_configService);
        return Task.FromResult(settings);
    }

    public async Task SaveSystemSettings(SystemConfigSettings settings)
    {
        settings.Save(_configService);

        // Now update the services with the new settings
        await _workService.SetCPUSchedule(settings.cpuSettings);

        // WP Settings have changed, so reset the client and token
        _wpService.ResetClient();

        _ = _notifier.NotifyClients(NotificationType.SystemSettingsChanged);
    }
}