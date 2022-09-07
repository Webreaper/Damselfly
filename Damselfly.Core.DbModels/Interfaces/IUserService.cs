using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IUserService
{
    public event Action<int?> OnUserIdChanged;

    int? UserId { get; }
    Task<bool> PolicyApplies(string policy);
    bool RolesEnabled { get; }
}

