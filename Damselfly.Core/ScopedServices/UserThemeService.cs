using System;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

// WASM Do we need this now?
public class UserThemeService
{
    private readonly UserConfigService _configService;
    private readonly IThemeService _themeService;

    public UserThemeService(UserConfigService configService, IThemeService themeService)
    {
        _configService = configService;
        _themeService = themeService;

        CurrentThemeName = _configService.Get(ConfigSettings.Theme, "green");
    }

    public string CurrentThemeName
    {
        get => CurrentTheme.Name;
        set
        {
            var newTheme = _themeService.GetThemeConfig(value).Result;

            if ( newTheme is not null )
            {
                CurrentTheme = newTheme;
                _configService.Set(ConfigSettings.Theme, value);
                OnChangeTheme?.Invoke(CurrentTheme);
            }
        }
    }

    public ThemeConfig CurrentTheme { get; private set; }

    public event Action<ThemeConfig> OnChangeTheme;
}