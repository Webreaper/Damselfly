using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;

namespace Damselfly.Core.ScopedServices;

public class ClientUserService : BaseClientService
{
    public ClientUserService(HttpClient client) : base(client) { }

    public AppIdentityUser User
    {
        get { return null; }
    }
}

