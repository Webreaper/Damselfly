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
using Damselfly.Core.DbModels.Models.APIModels;
using System.Data;

namespace Damselfly.Core.ScopedServices;

public class ClientUserMgmtService : IUserMgmtService
{
    private readonly RestClient httpClient;

    public ClientUserMgmtService(RestClient client)
    {
        httpClient = client;
    }

    // WASM: TODO
    public bool RolesEnabled { get { return true; } }

    // WASM: should this be here?
    public bool AllowPublicRegistration => true;

    public async Task<ICollection<AppIdentityUser>> GetUsers()
    {
        return await httpClient.CustomGetFromJsonAsync<ICollection<AppIdentityUser>>("/api/users");
    }

    public async Task<IdentityResult> UpdateUserAsync(AppIdentityUser user, ICollection<string> newRoles)
    {
        var req = new UserRequest { User = user, Roles = newRoles };
        var result = await httpClient.CustomPostAsJsonAsync<UserRequest, IdentityResult>("/api/users", req);
        return result;
    }

    public async Task<IdentityResult> SetUserPasswordAsync(AppIdentityUser user, string password)
    {
        var req = new UserRequest { User = user, Password = password };
        var result = await httpClient.CustomPostAsJsonAsync<UserRequest, IdentityResult>("/api/users", req);
        return result;
    }

    public async Task<IdentityResult> CreateNewUser(AppIdentityUser newUser, string password, ICollection<string> roles = null)
    {
        // /api/users
        var req = new UserRequest { User = newUser, Password = password, Roles = roles };
        var result = await httpClient.CustomPutAsJsonAsync<UserRequest, IdentityResult>("/api/users", req);

        return result;
    }

    public async Task<ICollection<ApplicationRole>> GetRoles()
    {
        return await httpClient.CustomGetFromJsonAsync<ICollection<ApplicationRole>>("/api/users/roles");
    }

    public Task AddUserToDefaultRoles(AppIdentityUser user)
    {
        throw new NotImplementedException();
    }
}

