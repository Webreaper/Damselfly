using Damselfly.Core.DbModels;
using Damselfly.Core.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.Services
{
    public class UserConfigService
    {
        private AuthenticationStateProvider _authenticationStateProvider;
        private ConfigService _configService;
        private UserManager<AppIdentityUser> _userManager;

        public UserConfigService(AuthenticationStateProvider authenticationStateProvider, ConfigService configService,
                                    UserManager<AppIdentityUser> userManager )
        {
            _authenticationStateProvider = authenticationStateProvider;
            _configService = configService;
            _userManager = userManager;
        }

        public string Get(string name, string defaultIfNotExists = null)
        {
            var authState = _authenticationStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult();
            var user = _userManager.GetUserAsync(authState.User).GetAwaiter().GetResult(); ;

            return _configService.Get(name, defaultIfNotExists, user);
        }

        public EnumType Get<EnumType>(string name, EnumType defaultIfNotExists = default, IDamselflyUser user = null) where EnumType : struct
        {
            throw new System.NotImplementedException();
        }

        public bool GetBool(string name, bool defaultIfNotExists = false, IDamselflyUser user = null)
        {
            throw new System.NotImplementedException();
        }

        public int GetInt(string name, int defaultIfNotExists = 0, IDamselflyUser user = null)
        {
            throw new System.NotImplementedException();
        }

        public void Set(string name, string value, IDamselflyUser user = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
