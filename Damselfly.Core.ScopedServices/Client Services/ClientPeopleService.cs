using System;
using Damselfly.Core.DbModels;
using System.Net.Http.Json;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;

namespace Damselfly.Core.ScopedServices;

public class ClientPeopleService 
{
    private readonly RestClient httpClient;

    public ClientPeopleService( RestClient client ) 
    {
        httpClient = client;
    }

    public async Task<List<string>> GetPeopleNames(string searchText)
    {
        return await httpClient.CustomGetFromJsonAsync<List<string>>($"/api/people/{searchText}");
    }

    public async Task UpdateName(ImageObject theObject, string newName)
    {
        await httpClient.CustomPutAsJsonAsync<string>($"/api/people/name/{theObject.ImageObjectId}", newName);
    }

    public async Task UpdatePerson(Person thePerson, string newName)
    {
        await httpClient.CustomPutAsJsonAsync<string>($"/api/people/name/{thePerson.PersonId}", newName);
    }
}

