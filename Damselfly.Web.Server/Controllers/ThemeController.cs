using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/theme")]
[Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
public class ThemeController : ControllerBase
{
    private readonly ILogger<ThemeController> _logger;
    private readonly IThemeService _service;

    public ThemeController(IThemeService service, ILogger<ThemeController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("/api/theme")]
    public async Task<ThemeConfig> GetDefaultTheme()
    {
        return await _service.GetThemeConfig("green");
    }

    [HttpGet("/api/themes")]
    public async Task<List<ThemeConfig>> GetAllThemes()
    {
        return await _service.GetAllThemes();
    }

    [HttpGet("/api/theme/{name}")]
    public async Task<ThemeConfig?> Get(string name)
    {
        return await _service.GetThemeConfig(name);
    }
}