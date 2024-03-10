using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IPeopleService
{
    Task<Person> GetPerson(int personId);
    Task<List<Person>> GetAllPeople();
    Task<List<string>> GetPeopleNames(string searchText);
    Task UpdatePersonName(NameChangeRequest req);
    Task<bool> NeedsAIMigration();
    Task ExecuteAIMigration( AIMigrationRequest req );
}