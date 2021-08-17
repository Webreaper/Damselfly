using System;
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

        /// <summary>
        /// Temp hack to get an admin user setup. TODO: Need to figure this out
        /// </summary>
        /// <param name="userManager"></param>
        /// <returns></returns>
        public async Task CheckAdminUser()
        {
            try
            {
                var adminRole = await _roleManager.FindByNameAsync(RoleDefinitions.s_AdminRole);

                if (adminRole != null)
                {
                    var users = _userManager.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).ToList(); ;

                    if( ! users.Any( x => x.UserRoles.Any( y => y.Role.Id == adminRole.Id )))
                    {
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
                else
                    throw new InvalidOperationException("No Admin role found in database. Setup error.");
            }
            catch( Exception ex)
            {
                Logging.LogError($"Unexpected exception while checking Admin role members: {ex}");
            }
        }
    }
}
