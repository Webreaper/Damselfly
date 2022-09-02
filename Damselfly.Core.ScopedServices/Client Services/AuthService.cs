using Blazored.LocalStorage;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Damselfly.Core.ScopedServices.ClientServices;

public class AuthService : IAuthService
{
    private readonly RestClient _httpClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
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
        var result = await _httpClient.CustomPostAsJsonAsync<RegisterModel, RegisterResult>("api/accounts", registerModel);
        return result;
    }

    public async Task<LoginResult> Login(LoginModel loginModel)
    {
        var loginResult = await _httpClient.CustomPostAsJsonAsync<LoginModel, LoginResult>("api/Login", loginModel);
        var provider = _authenticationStateProvider as ApiAuthenticationStateProvider;

        if (loginResult.Successful)
        {
            await _localStorage.SetItemAsync("authToken", loginResult.Token);
            provider.MarkUserAsAuthenticated(loginModel.Email);
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