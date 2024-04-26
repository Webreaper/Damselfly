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
    private readonly IAuthService _authService;

    public LoginController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginModel login)
    {
        return BadRequest();
        var result = await _authService.Login( login );

        if( result.Successful )
            return Ok( result );
        else
            return BadRequest( result );
    }
}