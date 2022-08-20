using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.ScopedServices.ClientServices;

namespace Damselfly.Core.ScopedServices;

public class ClientUserService : IUserService
{
    private readonly RestClient httpClient;

    public ClientUserService(RestClient client)
    {
        httpClient = client;
    }

    public AppIdentityUser User
    {
        get { return null; }
    }

    // WASM: TODO
    public bool RolesEnabled { get { return false; } }

    // WASM: TODO
    public async Task<bool> PolicyApplies( string policy )
    {
        return false;
    }

    // WASM: TODO:
    public async Task<ICollection<AppIdentityUser>> GetUsers()
    {
        return new List<AppIdentityUser>();
    }
}

