using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Authentication;

namespace Damselfly.Web.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly SignInManager<AppIdentityUser> _signInManager;

    public LoginController(IConfiguration configuration,
                           SignInManager<AppIdentityUser> signInManager)
    {
        _configuration = configuration;
        _signInManager = signInManager;
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginModel login)
    {
        var user = await _signInManager.UserManager.FindByEmailAsync(login.Email);

        if (user == null)
            return BadRequest(new LoginResult { Successful = false, Error = "Username or password was invalid." });

        var result = await _signInManager.PasswordSignInAsync(user.UserName, login.Password, login.RememberMe, false);

        if (!result.Succeeded)
            return BadRequest(new LoginResult { Successful = false, Error = "Username or password was invalid." });

        var roles = await _signInManager.UserManager.GetRolesAsync(user);
        var claims = new List<Claim>();

        claims.Add(new Claim(ClaimTypes.Name, login.Email));

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("BlahSomeKeyBlahFlibbertyGibbertNonsenseBananarama"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.Now.AddDays(Convert.ToInt32(1));

        var token = new JwtSecurityToken(
            "https://localhost",
            "https://localhost",
            claims,
            expires: expiry,
            signingCredentials: creds
        );

        return Ok(new LoginResult { Successful = true, Token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}