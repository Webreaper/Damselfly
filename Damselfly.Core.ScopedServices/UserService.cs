using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.ScopedServices.ClientServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Damselfly.Core.Utils;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Service used by both Blazor WASM and Blazor Server
/// </summary>
public class UserService : IUserService, IDisposable
{
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IAuthorizationService _authService;
    private bool _initialised = false;
    private AppIdentityUser _user = null;
    public event Action<AppIdentityUser> OnUserChanged;

    public UserService(AuthenticationStateProvider authenticationStateProvider,
                    IAuthorizationService authService,
                    UserManager<AppIdentityUser> userManager)
    {
        _userManager = userManager;
        _authenticationStateProvider = authenticationStateProvider;

        _authenticationStateProvider.AuthenticationStateChanged += AuthStateChanged;
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= AuthStateChanged;
    }

    public bool RolesEnabled => true;

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
                    var authState = _authenticationStateProvider.GetAuthenticationStateAsync().Result;
                    _user = _userManager.GetUserAsync(authState.User).Result;
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
    private async void AuthStateChanged(Task<AuthenticationState> task)
    {
        var authState = await task;

        _user = await _userManager.GetUserAsync(authState.User);

        if (_user != null)
            Logging.Log($"User changed to {_user.UserName}");
        else
            Logging.Log($"User state changed to logged out");

        OnUserChanged?.Invoke(_user);
    }


    /// <summary>
    /// Returns true if the policy passed in applies to the currently
    /// logged-in user.
    /// </summary>
    /// <param name="policy"></param>
    /// <returns></returns>
    public async Task<bool> PolicyApplies(string policy)
    {
        if (!RolesEnabled)
            return true;

        if (_user == null)
            return false;

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();

        var result = await _authService.AuthorizeAsync(authState.User, policy);

        return result.Succeeded;
    }
}

