using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IThemeService
{
    event Action<ThemeConfig> OnChangeTheme;
    Task<ThemeConfig> GetDefaultTheme();
    Task<ThemeConfig> GetThemeConfig(string name);
    Task<List<ThemeConfig>> GetAllThemes();
}

