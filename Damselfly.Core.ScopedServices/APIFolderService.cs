using System;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class APIFolderService : IFolderService
{
    public event Action OnChange;

    public async Task<List<Folder>> GetFilteredFolders(string filterTerm)
    {
        throw new NotImplementedException();
    }

    public void ToggleExpand(Folder item)
    {
        throw new NotImplementedException();
    }
}

