using System;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

// WASM: TODO: Do we even need this?
public interface IUserFolderService
{
    event Action OnFoldersChanged;
    Task<List<Folder>> GetFilteredFolders(string filterTerm);
    void ToggleExpand(Folder item);
    bool IsExpanded(Folder item);
}

