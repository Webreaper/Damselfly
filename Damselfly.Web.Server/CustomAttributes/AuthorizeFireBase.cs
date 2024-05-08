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
    public class AuthorizeFireBase(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<AuthorizeFireBase>, IAuthorizationRequirement
    {

        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

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
           var isAuthenticated = await authService.CheckCurrentFirebaseUserIsInRole([]);
            if( !isAuthenticated )
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            return;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizeFireBase requirement)
        {
            
            var user = context.User;
            if( !user.Identity.IsAuthenticated )
            {
                context.Fail();
            }
            var authService = _httpContextAccessor.HttpContext
                .RequestServices
                .GetService(typeof(IAuthService)) as IAuthService;
            var isAuthenticated = await authService.CheckCurrentFirebaseUserIsInRole([]);
            if( !isAuthenticated )
            {
                context.Fail();
            }
            context.Succeed(requirement);
        }
    }
}
