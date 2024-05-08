using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.DbModels;

public static class AuthorisationRegistration
{
    private static bool IsAdminOrNoUsers( AuthorizationHandlerContext context, UserManager<AppIdentityUser> userManager )
    {
        if (context.User != null && context.User.IsInRole(RoleDefinitions.s_AdminRole) )
            return true;

        // No logged in users. See if there are any users. If not, we allow it
        if (userManager != null && !userManager.Users.Any())
            return true;

        return false;
    }

    /// <summary>
    ///     TODO: This can probably go somewhere better
    /// </summary>
    /// <param name="config"></param>
    /// <param name="services"></param>
    public static void SetupPolicies(this AuthorizationOptions config, IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var configService = serviceProvider.GetService<IConfigService>();
        var userManager = serviceProvider.GetService<UserManager<AppIdentityUser>>();
        var logger = serviceProvider.GetService<ILogger<AuthorizationOptions>>();
        var enablePolicies = configService!.GetBool(ConfigSettings.EnablePoliciesAndRoles,
                                ConfigSettings.DefaultEnableRolesAndAuth);


        if ( enablePolicies )
        {
            logger.LogInformation("Polices and Roles are enabled.");

            // Anyone in the Admin group is an admin (d'uh)
            config.AddPolicy(PolicyDefinitions.s_IsLoggedIn, policy => policy.RequireAuthenticatedUser());

            // Anyone in the Admin group is an admin (d'uh)
            config.AddPolicy(PolicyDefinitions.s_IsAdmin, policy => policy.RequireRole(
                RoleDefinitions.s_AdminRole));

            // Users and Admins can edit content (keywords)
            config.AddPolicy(PolicyDefinitions.s_IsEditor, policy => policy.RequireRole(
                      RoleDefinitions.s_AdminRole,
                      RoleDefinitions.s_UserRole));

            // Special role for the user Admin page - only accessible if the current user
            // is an admin or there are no users defined at all.
            config.AddPolicy(PolicyDefinitions.s_IsAdminOrNoUsers, policy => policy.RequireAssertion(
                            context => IsAdminOrNoUsers( context, userManager )));

            // Admins, Users and ReadOnly users can download
            config.AddPolicy(PolicyDefinitions.s_IsDownloader, policy => policy.RequireRole(
                RoleDefinitions.s_AdminRole,
                RoleDefinitions.s_UserRole,
                RoleDefinitions.s_ReadOnlyRole));

            
        }
        else
        {
            logger.LogInformation("Polices and Roles have been deactivated.");

            config.AddPolicy(PolicyDefinitions.s_IsLoggedIn, policy => policy.RequireAssertion(
                context => true));
            config.AddPolicy(PolicyDefinitions.s_IsAdmin, policy => policy.RequireAssertion(
                context => true));
            config.AddPolicy(PolicyDefinitions.s_IsAdminOrNoUsers, policy => policy.RequireAssertion(
                context => true));
            config.AddPolicy(PolicyDefinitions.s_IsEditor, policy => policy.RequireAssertion(
                context => true));
            config.AddPolicy(PolicyDefinitions.s_IsDownloader, policy => policy.RequireAssertion(
                context => true));
        }
    }
}