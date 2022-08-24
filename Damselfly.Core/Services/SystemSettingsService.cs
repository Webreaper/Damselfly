using System;
using System.Linq;
using Damselfly.Core.Models;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Core.ScopedServices;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Damselfly.Core.ScopedServices.Interfaces;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.DbModels.Models;
using Damselfly.ML.Face.Azure;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Core.Services;

/// <summary>
/// Service to store NVP configuration settings
/// </summary>
public class SystemSettingsService : ISystemSettingsService
{
    private readonly ILogger<SystemSettingsService> _logger;
    private readonly IConfigService _configService;
    private readonly WorkService _workService;
    private readonly AzureFaceService _azureService;
    private readonly WordpressService _wpService;
    private readonly ServerNotifierService _notifier;

    public SystemSettingsService( ILogger<SystemSettingsService> logger,
                                    WorkService workService,
                                    AzureFaceService azureService,
                                    WordpressService wpService,
                                    IConfigService configService)
    {
        _workService = workService;
        _azureService = azureService;
        _wpService = wpService;
        _configService = configService;
        _logger = logger;
    }

    public async Task<SystemConfigSettings> GetSystemSettings()
    {
        var settings = new SystemConfigSettings();
        settings.Load(_configService);
        return settings;
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
    }
}
