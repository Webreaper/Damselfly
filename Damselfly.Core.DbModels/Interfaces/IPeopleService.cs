using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IPeopleService
{
    Task UpdatePerson(Person thePerson, string newName);
    Task<List<string>> GetPeopleNames(string searchText);
    Task UpdateName(ImageObject theObject, string newName);
}

