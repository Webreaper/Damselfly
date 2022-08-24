using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.ScopedServices.ClientServices;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Damselfly.Core.ScopedServices;

public class TempHackUserService : IUserService
{
    public TempHackUserService()
    {
    }

    public AppIdentityUser User
    {
        get { return null; }
    }

    // WASM: TODO
    public bool RolesEnabled { get { return true; } }

    // WASM: TODO
    public async Task<bool> PolicyApplies(string policy)
    {
        return false;
    }

    // WASM: TODO:
    public async Task<ICollection<AppIdentityUser>> GetUsers()
    {
        return new List<AppIdentityUser>();
    }
    public Task<IdentityResult> UpdateUserAsync(AppIdentityUser user, string newRole)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> SetUserPasswordAsync(AppIdentityUser user, string password)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> CreateNewUser(AppIdentityUser newUser, string password, ICollection<string> roles = null)
    {
        throw new NotImplementedException();
    }

    public async Task<ICollection<ApplicationRole>> GetRoles()
    {
        return new List<ApplicationRole>();
    }
}

