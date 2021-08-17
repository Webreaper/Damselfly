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
        private AuthenticationStateProvider _authenticationStateProvider;
        private AppIdentityUser _user;
        private bool _initialised;
        public Action<AppIdentityUser> OnChange;

        public UserService(AuthenticationStateProvider authenticationStateProvider,
                                RoleManager<ApplicationRole> roleManager,
                                UserManager<AppIdentityUser> userManager)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _userManager = userManager;
            _roleManager = roleManager;
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

        public async Task<bool> UpdateUserAsync( AppIdentityUser user )
        {
            var result = await _userManager.UpdateAsync(user);

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
                var adminUsers = await _userManager.GetUsersInRoleAsync(RoleDefinitions.s_AdminRole);

                if( !adminUsers.Any() )
                {
                    var user = _userManager.Users.MinBy(x => x.Id);

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
            catch( Exception ex)
            {
                Logging.LogError($"Unexpected exception while checking Admin role members: {ex}");
            }
        }
    }
}
