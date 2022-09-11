using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IUserFolderService
{
    event Action OnFoldersChanged;
    Task<List<Folder>> GetFilteredFolders(string filterTerm);
    void ToggleExpand(Folder item);
    bool IsExpanded(Folder item);
}

