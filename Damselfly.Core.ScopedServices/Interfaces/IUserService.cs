using System;
using Damselfly.Core.DbModels;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IUserService
{
    AppIdentityUser User { get; }
    bool RolesEnabled { get; }
    Task<bool> PolicyApplies(string policy);
    Task<ICollection<AppIdentityUser>> GetUsers();
}

