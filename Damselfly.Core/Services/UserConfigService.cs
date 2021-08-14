using System;
using Damselfly.Core.DbModels;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.Services
{
    public class UserConfigService
    {
        private AuthenticationStateProvider _authenticationStateProvider;
        private ConfigService _configService;
        private UserManager<AppIdentityUser> _userManager;
        private AppIdentityUser _user => GetUser();

        public UserConfigService(AuthenticationStateProvider authenticationStateProvider, ConfigService configService,
                                    UserManager<AppIdentityUser> userManager )
        {
            _authenticationStateProvider = authenticationStateProvider;
            _configService = configService;
            _userManager = userManager;

        }

        private AppIdentityUser GetUser()
        {
            try
            {
                // TODO: Not sure this is a great place to do this async bodge
                var authState = _authenticationStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult();
                return _userManager.GetUserAsync(authState.User).GetAwaiter().GetResult();
            }
            catch( Exception ex )
            {
                Logging.LogError( $"Identity State error: {ex.Message}");
                return null;
            }
        }

        public string Get(string name, string defaultIfNotExists = null)
        {
            return _configService.Get(name, defaultIfNotExists, _user);
        }

        public EnumType Get<EnumType>(string name, EnumType defaultIfNotExists = default, IDamselflyUser user = null) where EnumType : struct
        {
            return _configService.Get(name, defaultIfNotExists, _user);
        }

        public bool GetBool(string name, bool defaultIfNotExists = false, IDamselflyUser user = null)
        {
            return _configService.GetBool(name, defaultIfNotExists, _user);
        }

        public int GetInt(string name, int defaultIfNotExists = 0, IDamselflyUser user = null)
        {
            return _configService.GetInt(name, defaultIfNotExists, _user);
        }

        public void Set(string name, string value, IDamselflyUser user = null)
        {
            _configService.Set(name, value, _user);
        }
    }
}
