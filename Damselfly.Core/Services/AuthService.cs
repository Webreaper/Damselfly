using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models.Enums;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Damselfly.Core.Services;

public class AuthService : IAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;


    public AuthService( IHttpContextAccessor httpContextAccessor, IServiceScopeFactory scopeFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
    }


    public async Task<LoginResult> Login(LoginModel login)
    {
        //var user = await _signInManager.UserManager.FindByEmailAsync( login.Email );

        //if( user == null )
        //    return new LoginResult { Successful = false, Error = "Username or password was invalid." };

        //var result = await _signInManager.PasswordSignInAsync( user.UserName, login.Password, login.RememberMe, false );

        //if( !result.Succeeded )
        //    return new LoginResult { Successful = false, Error = "Username or password was invalid." };

        //var roles = await _signInManager.UserManager.GetRolesAsync( user );
        //var claims = new List<Claim>
        //{
        //    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        //    new Claim(ClaimTypes.Email, login.Email),
        //    new Claim(ClaimTypes.Name, user.UserName)
        //};

        //foreach( var role in roles )
        //    claims.Add( new Claim( ClaimTypes.Role, role ) );

        //var key = new SymmetricSecurityKey( Encoding.UTF8.GetBytes( "BlahSomeKeyBlahFlibbertyGibbertNonsenseBananarama" ) );
        //var creds = new SigningCredentials( key, SecurityAlgorithms.HmacSha256 );
        //var expiry = DateTime.Now.AddDays( Convert.ToInt32( 1 ) );

        //var token = new JwtSecurityToken(
        //    "https://localhost",
        //    "https://localhost",
        //    claims,
        //    expires: expiry,
        //    signingCredentials: creds
        //);

        //return new LoginResult { Successful = true, Token = new JwtSecurityTokenHandler().WriteToken( token ) };
        throw new NotImplementedException();
    }

    public Task Logout()
    {
        throw new NotImplementedException();
    }

    public async Task<RegisterResult> Register( RegisterModel model)
    {
        //var newUser = new AppIdentityUser { UserName = model.Email, Email = model.Email };

        //var result = await _userManager.CreateAsync( newUser, model.Password );

        //if( !result.Succeeded )
        //{
        //    var errors = result.Errors.Select( x => x.Description );

        //    return new RegisterResult { Successful = false, Errors = errors };
        //}

        //return new RegisterResult { Successful = true };
        throw new NotImplementedException();
    }

    public async Task<bool> CheckCurrentFirebaseUserIsInRole(string[] roles)
    {
        var email = await GetCurrentUserEmail();
        if (email == null)
        {
            return false;
        }
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<ImageContext>();
        //var userManager = _userManager;
        var applicationUser = await db.Users.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.NormalizedEmail == email.ToUpper());
        // var applicationUser = await userManager.FindByEmailAsync(email.Value);
        if( applicationUser == null )
        {
            return false;
        }
        if( roles.Length == 0 )
        {
            return true;
        }
        //var userRoles = await userManager.GetRolesAsync(applicationUser);
        var hasRole = applicationUser.UserRoles.Any(userRole => roles.Any(role =>
        {
            var roleId = (int)RoleEnumExtensions.FromFriendlyString(role);
            return userRole.RoleId == roleId;
        }));
        return hasRole;
    }

    public async Task<string> GetCurrentUserEmail()
    {
        var user = _httpContextAccessor.HttpContext.User;
        if( user == null )
        {
            return null;
        }
        var email = user.Claims.FirstOrDefault(u => u.Type == DamselflyContants.EmailClaim);
        return email == null ? null : email.Value;
    }
}

