using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IFolderService
{
    Task<ICollection<Folder>> GetFolders();

    Task<Dictionary<int, UserFolderState>> GetUserFolderStates(int? userId);

    Task SaveFolderStates(IEnumerable<UserFolderState> newStates);

    event Action OnChange;
}