using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IUserService
{
    AppIdentityUser User { get; }
    bool RolesEnabled { get; }
    Task<bool> PolicyApplies(string policy);
    Task<ICollection<AppIdentityUser>> GetUsers();
    Task<IdentityResult> UpdateUserAsync(AppIdentityUser user, string newRole);
    Task<IdentityResult> SetUserPasswordAsync(AppIdentityUser user, string password);
    Task<IdentityResult> CreateNewUser(AppIdentityUser newUser, string password, ICollection<string> roles = null);
    Task<ICollection<ApplicationRole>> GetRoles();
}

