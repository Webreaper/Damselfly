using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.ScopedServices.ClientServices;
using System.Collections.Generic;
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
}

