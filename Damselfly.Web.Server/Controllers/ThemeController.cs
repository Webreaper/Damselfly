using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using MudBlazor;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Route("/api/theme")]
public class ThemeController : ControllerBase
{
    private readonly ThemeService _service;

    private readonly ILogger<ThemeController> _logger;

    public ThemeController(ThemeService service, ILogger<ThemeController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("/api/theme/{name}")]
    public async Task<ThemeConfig> Get( string name)
    {
        if (string.IsNullOrEmpty(name))
            name = "Green"; // Default

        var theme = _service.Themes.FirstOrDefault(x => x.Name == name);

        if( theme == null )
            theme = new ThemeConfig { MudTheme = _service.DarkTheme, Name = "Dark Theme", Path = String.Empty };

        return theme;
    }
}

