using System;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IFolderService
{
    event Action OnChange;
    Task<ICollection<Folder>> GetFilteredFolders(string filterTerm);
    void ToggleExpand(Folder item);
}

