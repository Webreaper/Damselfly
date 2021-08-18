using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Damselfly.Core.Services
{
    public class UserService
    {
        private UserManager<AppIdentityUser> _userManager;
        private RoleManager<ApplicationRole> _roleManager;
        private UserStatusService _statusService;
        private AuthenticationStateProvider _authenticationStateProvider;
        private AppIdentityUser _user;
        private bool _initialised;
        public Action<AppIdentityUser> OnChange;

        public UserService(AuthenticationStateProvider authenticationStateProvider,
                                RoleManager<ApplicationRole> roleManager,
                                UserManager<AppIdentityUser> userManager,
                                UserStatusService statusService)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _userManager = userManager;
            _roleManager = roleManager;
            _statusService = statusService;
        }

        private void AuthStateChanged(Task<AuthenticationState> newState)
        {
            var authState = newState.GetAwaiter().GetResult();
            _user = _userManager.GetUserAsync(authState.User).GetAwaiter().GetResult();

            if( _user != null )
                Logging.Log($"User changed to {_user.UserName}");
            else
                Logging.Log($"User state changed to logged out");

            OnChange?.Invoke(_user);
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
                    catch (Exception ex)
                    {
                        Logging.LogError($"Failed to get auth state.");
                    }
                }

                return _user;
            }
        }

        public async Task<ICollection<AppIdentityUser>> GetUsers()
        {
            var users = await _userManager.Users
                                    .Include( x => x.UserRoles )
                                    .ThenInclude( y => y.Role )
                                    .ToListAsync();
            return users;
        }

        public async Task<ICollection<ApplicationRole>> GetRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return roles;
        }

        public async Task<bool> UpdateUserAsync(AppIdentityUser user, ICollection<string> newRoleSet)
        {
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                if (await SyncUserRoles(user, newRoleSet))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Reset the user password
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password">Unhashed password</param>
        /// <returns></returns>
        public async Task<bool> SetUserPasswordAsync(AppIdentityUser user, string password)
        {
            string token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, token, password);

            return result.Succeeded;
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
                            await _userManager.AddToRoleAsync(user, RoleDefinitions.s_AdminRole);
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

        public async Task<bool> SyncUserRoles( AppIdentityUser user, ICollection<string> newRoles )
        {
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
            bool success = true;

            if (rolesToRemove.Any())
            {
                prefix = $"User {user.UserName} ";
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

                if (removeResult.Succeeded)
                {
                    changes = $"removed from {string.Join(", ", rolesToRemove.Select(x => $"'x'"))} roles";
                }
                else
                {
                    errorMsg = $"role removal failed: {removeResult.Errors}";
                    success = false;
                }
            }

            if (rolesToAdd.Any())
            {
                prefix = $"User {user.UserName} ";
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);

                if (!string.IsNullOrEmpty(changes))
                {
                    changes += " and ";
                }

                if (addResult.Succeeded)
                {
                    changes += $"added to {string.Join(", ", rolesToAdd.Select( x => $"'x'"))} roles";
                }
                else
                {
                    errorMsg = $"role addition failed: {addResult.Errors}";
                    success = false;
                }
            }

            if (!string.IsNullOrEmpty(changes))
                changes += ". ";

            _statusService.StatusText = $"{prefix}{changes}{errorMsg}";

            return success;
        }
    }
}
