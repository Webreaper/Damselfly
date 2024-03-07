using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class ClientPeopleService : IPeopleService
{
    private readonly RestClient httpClient;

    public ClientPeopleService(RestClient client)
    {
        httpClient = client;
    }

    public async Task<Person> GetPerson( int personId )
    {
        return await httpClient.CustomGetFromJsonAsync<Person>($"/api/person/{personId}");
    }

    public async Task<List<Person>> GetAllPeople()
    {
        return await httpClient.CustomGetFromJsonAsync<List<Person>>("/api/people");
    }

    public async Task<List<string>> GetPeopleNames(string searchText)
    {
        return await httpClient.CustomGetFromJsonAsync<List<string>>($"/api/people/{searchText}");
    }

    public async Task UpdateName(ImageObject theObject, string newName, bool merge)
    {
        var req = new NameChangeRequest { ObjectId = theObject.ImageObjectId, NewName = newName, Merge = merge};
        await httpClient.CustomPutAsJsonAsync($"/api/object/name", req);
    }

    public async Task UpdatePerson(Person thePerson, string newName)
    {
        throw new NotImplementedException();
        //await httpClient.CustomPutAsJsonAsync($"/api/people/name/{thePerson.PersonId}", newName);
    }
}