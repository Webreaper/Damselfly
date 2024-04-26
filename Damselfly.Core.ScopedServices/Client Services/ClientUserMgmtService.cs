using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Damselfly.Core.ScopedServices;

public class ClientUserMgmtService : IUserMgmtService
{
    private readonly IConfigService _configService;
    private readonly RestClient httpClient;

    public ClientUserMgmtService(RestClient client, IConfigService configService)
    {
        httpClient = client;
        _configService = configService;
    }

    public bool RolesEnabled =>
        _configService.GetBool(ConfigSettings.EnablePoliciesAndRoles, ConfigSettings.DefaultEnableRolesAndAuth);

    // WASM: should this be here?
    public bool AllowPublicRegistration => true;

    public async Task<ICollection<AppIdentityUser>> GetUsers()
    {
        return await httpClient.CustomGetFromJsonAsync<ICollection<AppIdentityUser>>("/api/users");
    }

    public async Task<AppIdentityUser> GetUser( int userId )
    {
        return await httpClient.CustomGetFromJsonAsync<AppIdentityUser>($"/api/user/{userId}");
    }

    //public async Task<UserResponse> UpdateUserAsync(string userName, string email, ICollection<string> newRoles)
    //{
    //    var req = new UserRequest { UserName = userName, Email = email, Roles = newRoles };
    //    var result = await httpClient.CustomPostAsJsonAsync<UserRequest, UserResponse>("/api/users", req);
    //    return result;
    //}

    //public async Task<UserResponse> SetUserPasswordAsync(string userName, string password)
    //{
    //    var req = new UserRequest { UserName = userName, Password = password };
    //    var result = await httpClient.CustomPostAsJsonAsync<UserRequest, UserResponse>("/api/users", req);
    //    return result;
    //}

    //public async Task<UserResponse> CreateNewUser(string userName, string email, string password,
    //    ICollection<string>? roles = null)
    //{
    //    // /api/users
    //    var req = new UserRequest { UserName = userName, Email = email, Password = password, Roles = roles };
    //    var result = await httpClient.CustomPutAsJsonAsync<UserRequest, UserResponse>("/api/users", req);

    //    return result;
    //}

    public async Task<ICollection<ApplicationRole>> GetRoles()
    {
        return await httpClient.CustomGetFromJsonAsync<ICollection<ApplicationRole>>("/api/users/roles");
    }

    public Task AddUserToDefaultRoles(AppIdentityUser user)
    {
        throw new NotImplementedException();
    }

    public Task<UserResponse> UpdateUserAsync(string userName, string email, ICollection<string> newRoles)
    {
        throw new NotImplementedException();
    }

    public Task<UserResponse> SetUserPasswordAsync(string userName, string password)
    {
        throw new NotImplementedException();
    }

    public Task<UserResponse> CreateNewUser(string userName, string email, string password, ICollection<string>? roles = null)
    {
        throw new NotImplementedException();
    }

    public Task<AppIdentityUser> GetOrCreateUser(ClaimsPrincipal user)
    {
        throw new NotImplementedException();
    }
}