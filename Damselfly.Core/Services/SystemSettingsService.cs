using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.ML.Face.Azure;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.Services;

/// <summary>
///     Service to store NVP configuration settings
/// </summary>
public class SystemSettingsService : ISystemSettingsService
{
    private readonly AzureFaceService _azureService;
    private readonly IConfigService _configService;
    private readonly ILogger<SystemSettingsService> _logger;
    private readonly ServerNotifierService _notifier;
    private readonly WorkService _workService;
    private readonly WordpressService _wpService;

    public SystemSettingsService(ILogger<SystemSettingsService> logger,
        WorkService workService,
        AzureFaceService azureService,
        WordpressService wpService,
        IConfigService configService,
        ServerNotifierService notifier)
    {
        _notifier = notifier;
        _workService = workService;
        _azureService = azureService;
        _wpService = wpService;
        _configService = configService;
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

        // Init the azure service status based on config.
        await _azureService.StartService();

        // WP Settings have changed, so reset the client and token
        _wpService.ResetClient();

        _ = _notifier.NotifyClients(NotificationType.SystemSettingsChanged);
    }
}