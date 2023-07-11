using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace Damselfly.Core.ScopedServices.ClientServices;

public class AuthService : IAuthService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly RestClient _httpClient;
    private readonly ILocalStorageService _localStorage;

    public AuthService(RestClient httpClient,
        AuthenticationStateProvider authenticationStateProvider,
        ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authenticationStateProvider = authenticationStateProvider;
        _localStorage = localStorage;
    }

    public async Task<RegisterResult> Register(RegisterModel registerModel)
    {
        var result =
            await _httpClient.CustomPostAsJsonAsync<RegisterModel, RegisterResult>("api/accounts", registerModel);
        return result;
    }

    public async Task<LoginResult> Login(LoginModel loginModel)
    {
        var loginResult = await _httpClient.CustomPostAsJsonAsync<LoginModel, LoginResult>("api/Login", loginModel);
        var provider = _authenticationStateProvider as ApiAuthenticationStateProvider;

        if ( loginResult != null && provider != null &&loginResult.Successful )
        {
            await _localStorage.SetItemAsync("authToken", loginResult.Token);

            // This will read the token from local storage and auth the user
            provider.MarkUserAsAuthenticated();

            // Set the token so the API controllers get it too
            _httpClient.AuthHeader = new AuthenticationHeaderValue("bearer", loginResult.Token);
        }

        return loginResult;
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
        _httpClient.AuthHeader = null;
    }
}