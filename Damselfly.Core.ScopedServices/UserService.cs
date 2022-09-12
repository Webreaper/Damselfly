using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Service used by both Blazor WASM and Blazor Server
/// </summary>
public class UserService : IUserService, IDisposable
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IAuthorizationService _authService;
    private int? _userId = null;
    public event Action<int?> OnUserIdChanged;

    public UserService(AuthenticationStateProvider authenticationStateProvider,
                         IAuthorizationService authService)
    {
        _authService = authService;
        _authenticationStateProvider = authenticationStateProvider;

        _authenticationStateProvider.AuthenticationStateChanged += AuthStateChanged;

        _ = GetCurrentUserId();
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= AuthStateChanged;
    }

    public bool RolesEnabled => true;
    public int? UserId => _userId;

    private async Task GetCurrentUserId()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        _userId = authState.GetUserIdFromPrincipal();
    }


    /// <summary>
    /// Handler for when the authentication state changes. 
    /// </summary>
    /// <param name="newState"></param>
    private async void AuthStateChanged(Task<AuthenticationState> authStateTask)
    {
        var authState = await authStateTask;
        _userId = authState.GetUserIdFromPrincipal();

        if (_userId is not null && _userId > 0 )
            Logging.Log($"User changed to {_userId}");
        else
            Logging.Log($"User state changed to logged out");

        OnUserIdChanged?.Invoke(_userId);
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

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();

        if ( _authService != null )
        {
            var result = await _authService.AuthorizeAsync( authState.User, policy );

            return result.Succeeded;
        }

        return false;
    }
}

