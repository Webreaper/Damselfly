using System;
using Damselfly.Core.Constants;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.DbModels;

public static class AuthorisationRegistration
{
    /// <summary>
    /// TODO: This can probably go somewhere better
    /// </summary>
    /// <param name="config"></param>
    /// <param name="services"></param>
    public static void SetupPolicies(this AuthorizationOptions config, IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var configService = serviceProvider.GetService<IConfigService>();
        var logger = serviceProvider.GetService<ILogger<AuthorizationOptions>>();
        var enablePolicies = configService.GetBool(ConfigSettings.EnablePoliciesAndRoles, true);

        if (enablePolicies)
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
            config.AddPolicy(PolicyDefinitions.s_IsEditor, policy => policy.RequireAssertion(
                                                            context => true));
            config.AddPolicy(PolicyDefinitions.s_IsDownloader, policy => policy.RequireAssertion(
                                                            context => true));
        }

    }
}

