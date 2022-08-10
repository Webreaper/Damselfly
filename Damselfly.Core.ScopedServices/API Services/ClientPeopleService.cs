using System;
using Damselfly.Core.DbModels;
using System.Net.Http.Json;

namespace Damselfly.Core.ScopedServices;

public class ClientPeopleService : BaseClientService
{
    public ClientPeopleService( HttpClient client ) : base( client )  {  }

    public async Task<List<string>> GetPeopleNames(string searchText)
    {
        return await httpClient.GetFromJsonAsync<List<string>>($"/api/people/{searchText}");
    }
}

