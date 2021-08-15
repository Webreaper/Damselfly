using System;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.Services
{
    public class UserService
    {
        private UserManager<AppIdentityUser> _userManager;
        private AuthenticationStateProvider _authenticationStateProvider;
        private AppIdentityUser _user;
        private bool _initialised;
        public Action<AppIdentityUser> OnChange;

        public UserService(AuthenticationStateProvider authenticationStateProvider, UserManager<AppIdentityUser> userManager)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _userManager = userManager;
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
    }
}
