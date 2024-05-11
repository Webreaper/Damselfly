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
    public class AuthorizeFireBase(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<AuthorizeFireBase>, IAuthorizationRequirement, IAuthorizationHandler
    {

        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public override async Task HandleAsync(AuthorizationHandlerContext context)
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
