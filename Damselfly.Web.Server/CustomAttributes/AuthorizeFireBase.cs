using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Damselfly.Web.Server.CustomAttributes
{
    public class AuthorizeFireBase : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public AuthorizeFireBase(params string[] roles)
        {
            _roles = roles;
        }

        public AuthorizeFireBase()
        {
            _roles = Array.Empty<string>();
        }

        public async void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if( !user.Identity.IsAuthenticated )
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            var authService = context.HttpContext
                .RequestServices
                .GetService(typeof(IAuthService)) as IAuthService;
           var isAuthenticated = _roles.Length == 0 || await authService.CheckCurrentFirebaseUserIsInRole(_roles);
            if( !isAuthenticated )
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            return;
        }
    }
}
