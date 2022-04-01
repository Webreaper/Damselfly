using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

// Avoid namespace clashes with IAuthorizationService extension methods
using AuthenticationStateProvider = Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider;
using AuthenticationState = Microsoft.AspNetCore.Components.Authorization.AuthenticationState;
using Microsoft.AspNetCore.Identity.UI.Services;
using Damselfly.Core.Services;
using Damselfly.Core.Utils.Constants;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// User service to manage users and roles. We try and keep each user to a distinct
/// role - so they're either an Admin, User or ReadOnly. Roles can be combinatorial
/// but it's simpler to have a single role per user.
/// </summary>
public class UserService
{
    private UserManager<AppIdentityUser> _userManager;
    private RoleManager<ApplicationRole> _roleManager;
    private UserStatusService _statusService;
    private ConfigService _configService;
    private IAuthorizationService _authService;
    private AuthenticationStateProvider _authenticationStateProvider;
    private AppIdentityUser _user;
    private bool _initialised;
    public Action<AppIdentityUser> OnChange;

    public UserService(AuthenticationStateProvider authenticationStateProvider,
                            RoleManager<ApplicationRole> roleManager,
                            UserManager<AppIdentityUser> userManager,
                            UserStatusService statusService,
                            ConfigService configService,
                            IAuthorizationService authService)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _userManager = userManager;
        _roleManager = roleManager;
        _authService = authService;
        _statusService = statusService;
        _configService = configService;
    }

    public AppIdentityUser User
    {
        get
        {
            if (!_initialised)
            {
                // Only do this once; Once we've initialised the first time,
                // all other updates are from the StateChanged notifier
                _initialised = true;
                _authenticationStateProvider.AuthenticationStateChanged += AuthStateChanged;

                try
                {
                    var authState = _authenticationStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult();
                    _user = _userManager.GetUserAsync(authState.User).GetAwaiter().GetResult();
                }
                catch
                {
                    // We don't care - this will happen before the auth state is established.
                }
            }

            return _user;
        }
    }

    /// <summary>
    /// Handler for when the authentication state changes. 
    /// </summary>
    /// <param name="newState"></param>
    private void AuthStateChanged(Task<AuthenticationState> newState)
    {
        var authState = newState.GetAwaiter().GetResult();
        _user = _userManager.GetUserAsync(authState.User).GetAwaiter().GetResult();

        if (_user != null)
            Logging.Log($"User changed to {_user.UserName}");
        else
            Logging.Log($"User state changed to logged out");

        OnChange?.Invoke(_user);
    }

    /// <summary>
    /// Returns true if the policy passed in applies to the currently
    /// logged-in user.
    /// </summary>
    /// <param name="policy"></param>
    /// <returns></returns>
    public async Task<bool> PolicyApplies( string policy )
    {
        if (!RolesEnabled)
            return true;

        if (_user == null)
            return false;

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();

        var result = await _authService.AuthorizeAsync(authState.User, policy);

        return result.Succeeded;
    }

    public /*async*/ Task<string> GetUserPasswordResetLink( AppIdentityUser user )
    {
        // http://localhost:6363/Identity/Account/ResetPassword?user=12345&code=2134234
       throw new NotImplementedException();
        /* Something like.... 
        var user = await UserManager.FindByNameAsync(model.Email);
        if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
        {
            // Don't reveal that the user does not exist or is not confirmed
            return View("ForgotPasswordConfirmation");
        }

        var code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
        var callbackUrl = Url.Action("ResetPassword", "Account",
                    new { UserId = user.Id, code = code }, protocol: Request.Url.Scheme);
        await UserManager.SendEmailAsync(user.Id, "Reset Password",
                        "Please reset your password by clicking here: <a href=\"" + callbackUrl + "\">link</a>");
        return View("ForgotPasswordConfirmation");
        */
    }

    public bool RolesEnabled
    {
        get
        {
            return _configService.GetBool(ConfigSettings.EnablePoliciesAndRoles);
        }
    }

    public bool AllowPublicRegistration
    {
        get {
            return _configService.GetBool(ConfigSettings.AllowExternalRegistration, false);
        }
    }

    /// <summary>
    /// Gets the list of users currently registered
    /// </summary>
    /// <returns></returns>
    public async Task<ICollection<AppIdentityUser>> GetUsers()
    {
        var users = await _userManager.Users
                                .Include( x => x.UserRoles )
                                .ThenInclude( y => y.Role )
                                .ToListAsync();
        return users;
    }

    /// <summary>
    /// Gets the list of roles configured in the system
    /// </summary>
    /// <returns></returns>
    public async Task<ICollection<ApplicationRole>> GetRoles()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return roles;
    }

    /// <summary>
    /// For newly registered users, check the role they're in
    /// and add them to the default role(s).
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task AddUserToDefaultRoles(AppIdentityUser user)
    {
        // First, if they're the first user in the DB, make them Admin
        await CheckAdminUser();

        var userRoles = await _userManager.GetRolesAsync(user);

        if (!userRoles.Any())
        {
            // If the user isn't a member of other roles (i.e., they haven't
            // been added to Admin) then make them a 'user'.
            await _userManager.AddToRoleAsync(user, RoleDefinitions.s_UserRole);
        }
    }

    /// <summary>
    /// Updates a user's properties and syncs their roles. 
    /// </summary>
    /// <param name="user"></param>
    /// <param name="newRoleSet"></param>
    /// <returns></returns>
    public async Task<IdentityResult> UpdateUserAsync(AppIdentityUser user, string newRole)
    {
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var syncResult = await SyncUserRoles(user, new List<string> { newRole });

            if( syncResult != null )
            {
                // Non-null result means we did something and it succeeded or failed.
                result = syncResult;
            }
        }

        return result;
    }

    /// <summary>
    /// Reset the user password
    /// </summary>
    /// <param name="user"></param>
    /// <param name="password">Unhashed password</param>
    /// <returns></returns>
    public async Task<IdentityResult> SetUserPasswordAsync(AppIdentityUser user, string password)
    {
        string token = await _userManager.GeneratePasswordResetTokenAsync(user);

        return await _userManager.ResetPasswordAsync(user, token, password);
    }

    /// <summary>
    /// If there are no admin users, make the Admin with the lowest ID
    /// an admin. 
    /// TODO: This is a bit arbitrary, and whilst a reasonable fallback
    /// it's not robust from a security perspective. A better option might
    /// be to fail at startup, and provide a command-line option to set
    /// a user to admin based on email address. 
    /// </summary>
    /// <param name="userManager"></param>
    /// <returns></returns>
    public async Task CheckAdminUser()
    {
        try
        {
            // First, check if there's any users at all yet.
            var users = _userManager.Users.ToList();

            if (users.Any())
            {
                // If we have users, see if any are Admins.
                var adminUsers = await _userManager.GetUsersInRoleAsync(RoleDefinitions.s_AdminRole);

                if (!adminUsers.Any())
                {
                    // For the moment, arbitrarily promote the first user to admin
                    var user = users.MinBy(x => x.Id);

                    if (user != null)
                    {
                        Logging.Log($"No user found with {RoleDefinitions.s_AdminRole} role. Adding user {user.UserName} to that role.");

                        // Put admin in Administrator role
                        var result = await _userManager.AddToRoleAsync(user, RoleDefinitions.s_AdminRole);

                        if (result.Succeeded)
                        {
                            // Remove the other roles from the users
                            await _userManager.RemoveFromRolesAsync(user, new List<string> { RoleDefinitions.s_ReadOnlyRole, RoleDefinitions.s_UserRole });
                        }
                    }
                    else
                        Logging.LogWarning($"No user found that could be promoted to {RoleDefinitions.s_AdminRole} role.");
                }
            }
        }
        catch( Exception ex)
        {
            Logging.LogError($"Unexpected exception while checking Admin role members: {ex}");
        }
    }

    public async Task<IdentityResult> CreateNewUser(AppIdentityUser newUser, string password, ICollection<string> roles = null)
    {
        var result = await _userManager.CreateAsync(newUser, password);

        if (result.Succeeded)
        {
            Logging.Log("User created a new account with password.");

            if (roles == null || !roles.Any())
                await AddUserToDefaultRoles(newUser);
            else
                await SyncUserRoles(newUser, roles);
        }

        return result;
    }

    /// <summary>
    /// Syncs the user's roles to the set of roles passed in. Note that
    /// if the 'Admin' role is removed, and there are no other admin users
    /// in the system, the user won't be removed from the Admin roles, to
    /// ensure we always have at least one Admin.
    /// Note that this method works for multiple roles, but we only want
    /// to have users with one role at a time.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="newRoles"></param>
    /// <returns></returns>
    public async Task<IdentityResult> SyncUserRoles( AppIdentityUser user, ICollection<string> newRoles )
    {
        IdentityResult result = null;
        var roles = await _userManager.GetRolesAsync( user );

        var rolesToRemove = roles.Except( newRoles );
        var rolesToAdd = newRoles.Except( roles );
        var errorMsg = string.Empty;

        if (rolesToRemove.Contains(RoleDefinitions.s_AdminRole))
        {
            // Don't remove from Admin unless there's another admin
            var adminUsers = await _userManager.GetUsersInRoleAsync(RoleDefinitions.s_AdminRole);

            if (adminUsers.Count <= 1)
            {
                rolesToRemove = rolesToRemove.Except(new List<string> { RoleDefinitions.s_AdminRole });
                errorMsg = $" Please ensure one other user has '{RoleDefinitions.s_AdminRole}'.";
            }
        }

        string changes = string.Empty, prefix = string.Empty;

        if (rolesToRemove.Any())
        {
            prefix = $"User {user.UserName} ";
            result = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            if (result.Succeeded)
            {
                changes = $"removed from {string.Join(", ", rolesToRemove.Select(x => $"'x'"))} roles";
            }
            else
            {
                errorMsg = $"role removal failed: {result.Errors}";
            }
        }

        if( (result == null || result.Succeeded) && rolesToAdd.Any())
        {
            prefix = $"User {user.UserName} ";
            result = await _userManager.AddToRolesAsync(user, rolesToAdd);

            if (!string.IsNullOrEmpty(changes))
            {
                changes += " and ";
            }

            if (result.Succeeded)
            {
                changes += $"added to {string.Join(", ", rolesToAdd.Select( x => $"'x'"))} roles";
            }
            else
            {
                errorMsg = $"role addition failed: {result.Errors}";
            }
        }

        if (!string.IsNullOrEmpty(changes))
            changes += ". ";

        _statusService.StatusText = $"{prefix}{changes}{errorMsg}";
        Logging.Log( $"SyncUserRoles: {prefix}{changes}{errorMsg}");

        return result;
    }
}
