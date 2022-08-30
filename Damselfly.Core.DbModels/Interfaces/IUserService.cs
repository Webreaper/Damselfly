using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IUserService
{
    public event Action<AppIdentityUser> OnUserChanged;

    AppIdentityUser User { get; }
    Task<bool> PolicyApplies(string policy);
    bool RolesEnabled { get;  }
}

