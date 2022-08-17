using System;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.Interfaces;
using static Damselfly.Core.ScopedServices.ThemeService;

namespace Damselfly.Core.ScopedServices;

public class UserThemeService
{
    private readonly UserConfigService _configService;
    private readonly IThemeService _themeService;
    private ThemeConfig _currentTheme;
    public event Action<ThemeConfig> OnChangeTheme;

    public UserThemeService( UserConfigService configService, IThemeService themeService)
    {
        _configService = configService;
        _themeService = themeService;

        var userTheme = _configService.Get(ConfigSettings.Theme, "green");
        _currentTheme = _themeService.GetThemeConfig(userTheme).Result;
    }

    public string CurrentThemeName
    {
        get
        {
            return _currentTheme.Name;
        }
        set
        {
            _currentTheme = _themeService.GetThemeConfig(value).Result;
            _configService.Set(ConfigSettings.Theme, value);
            OnChangeTheme?.Invoke(_currentTheme);
        }
    }

    public ThemeConfig CurrentTheme
    {
        get { return _currentTheme; }
    }
}

