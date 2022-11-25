using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IThemeService
{
    event Action<ThemeConfig> OnChangeTheme;
    Task<ThemeConfig> GetDefaultTheme();
    Task<ThemeConfig> GetThemeConfig(string name);
    Task<List<ThemeConfig>> GetAllThemes();
    Task ApplyTheme(ThemeConfig newTheme);
    Task ApplyTheme(string themeName);
}