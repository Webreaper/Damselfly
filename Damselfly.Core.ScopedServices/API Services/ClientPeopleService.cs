using System;
using Damselfly.Core.DbModels;
using System.Net.Http.Json;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices;

public class ClientPeopleService : BaseClientService
{
    public ClientPeopleService( HttpClient client ) : base( client )  {  }

    public async Task<List<string>> GetPeopleNames(string searchText)
    {
        return await httpClient.GetFromJsonAsync<List<string>>($"/api/people/{searchText}");
    }

    public async Task UpdateName(ImageObject theObject, string newName)
    {
        await httpClient.PutAsJsonAsync<string>($"/api/people/name/{theObject.ImageObjectId}", newName);
    }
}

