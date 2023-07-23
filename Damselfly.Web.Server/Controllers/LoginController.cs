using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Damselfly.Web.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IAuthService _authService;
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly ILogger<LoginController> _logger;

    public LoginController(IConfiguration configuration,
        IAuthService authService,
         ILogger<LoginController> logger)
    {
        _configuration = configuration;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginModel login)
    {
        var result = await _authService.Login( login );

        if( result.Successful )
            return Ok( result );
        else
            return BadRequest( result );
    }
}