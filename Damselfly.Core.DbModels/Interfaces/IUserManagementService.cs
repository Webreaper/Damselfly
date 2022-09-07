using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IUserMgmtService
{
    bool RolesEnabled { get; }
    bool AllowPublicRegistration { get; } // WASM: Do we need this?
    Task<ICollection<AppIdentityUser>> GetUsers();
    Task<IdentityResult> UpdateUserAsync(AppIdentityUser user, ICollection<string> newRoles);
    Task<IdentityResult> SetUserPasswordAsync(AppIdentityUser user, string password);
    Task<IdentityResult> CreateNewUser(AppIdentityUser newUser, string password, ICollection<string> roles = null);
    Task<ICollection<ApplicationRole>> GetRoles();
    Task AddUserToDefaultRoles(AppIdentityUser user);
}

