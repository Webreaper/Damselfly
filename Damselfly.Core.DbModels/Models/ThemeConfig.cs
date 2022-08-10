using System;
using MudBlazor;

namespace Damselfly.Core.DbModels;

public class ThemeConfig
{
    public string Name { get; set; }
    public string Path { get; set; }
    public MudTheme MudTheme { get; set; }
}

