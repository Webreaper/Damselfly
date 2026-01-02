using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class UserConfigService : BaseConfigService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUserService _userService;

    public UserConfigService(IUserService userService, IServiceScopeFactory scopeFactory,
        ILogger<IConfigService> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _userService = userService;
        _userService.OnUserIdChanged += UserChanged;

        _ = base.InitialiseCache();
    }

    public void Dispose()
    {
        _userService.OnUserIdChanged -= UserChanged;
    }

    private void UserChanged(int? userId)
    {
        _ = InitialiseCache();
    }

    protected override async Task<List<ConfigSetting>> LoadAllSettings()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var settings = await db.ConfigSettings.Where(x => x.UserId == _userService.UserId).ToListAsync();

        return settings;
    }
}