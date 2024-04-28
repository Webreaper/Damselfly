using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Damselfly.Core.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly SignInManager<AppIdentityUser> _signInManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService( UserManager<AppIdentityUser> userManager,
                SignInManager<AppIdentityUser> signInManager, IHttpContextAccessor httpContextAccessor)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<LoginResult> Login(LoginModel login)
    {
        var user = await _signInManager.UserManager.FindByEmailAsync( login.Email );

        if( user == null )
            return new LoginResult { Successful = false, Error = "Username or password was invalid." };

        var result = await _signInManager.PasswordSignInAsync( user.UserName, login.Password, login.RememberMe, false );

        if( !result.Succeeded )
            return new LoginResult { Successful = false, Error = "Username or password was invalid." };

        var roles = await _signInManager.UserManager.GetRolesAsync( user );
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, login.Email),
            new Claim(ClaimTypes.Name, user.UserName)
        };

        foreach( var role in roles )
            claims.Add( new Claim( ClaimTypes.Role, role ) );

        var key = new SymmetricSecurityKey( Encoding.UTF8.GetBytes( "BlahSomeKeyBlahFlibbertyGibbertNonsenseBananarama" ) );
        var creds = new SigningCredentials( key, SecurityAlgorithms.HmacSha256 );
        var expiry = DateTime.Now.AddDays( Convert.ToInt32( 1 ) );

        var token = new JwtSecurityToken(
            "https://localhost",
            "https://localhost",
            claims,
            expires: expiry,
            signingCredentials: creds
        );

        return new LoginResult { Successful = true, Token = new JwtSecurityTokenHandler().WriteToken( token ) };
    }

    public Task Logout()
    {
        throw new NotImplementedException();
    }

    public async Task<RegisterResult> Register( RegisterModel model)
    {
        var newUser = new AppIdentityUser { UserName = model.Email, Email = model.Email };

        var result = await _userManager.CreateAsync( newUser, model.Password );

        if( !result.Succeeded )
        {
            var errors = result.Errors.Select( x => x.Description );

            return new RegisterResult { Successful = false, Errors = errors };
        }

        return new RegisterResult { Successful = true };
    }

    public async Task<bool> CheckCurrentFirebaseUserIsInRole(string[] roles)
    {
        var user = _httpContextAccessor.HttpContext.User;
        if (user == null)
        {
            return false;
        }
        var email = user.Claims.FirstOrDefault( u => u.Type == DamselflyContants.EmailClaim );
        if( email == null )
        {
            return false;
        }
        var userManager = _userManager;
        var applicationUser = await userManager.FindByEmailAsync(email.Value);
        if( applicationUser == null )
        {
            return false;
        }
        if( roles.Length == 0 )
        {
            return true;
        }
        var userRoles = await userManager.GetRolesAsync(applicationUser);
        var hasRole = userRoles.Any(r => roles.Contains(r));
        return hasRole;
    }
}

