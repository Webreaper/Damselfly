using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models.APIModels;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IUserMgmtService
{
    bool RolesEnabled { get; }
    bool AllowPublicRegistration { get; } // WASM: Do we need this?
    Task<ICollection<AppIdentityUser>> GetUsers();
    Task<AppIdentityUser> GetUser( int userId );
    Task<UserResponse> UpdateUserAsync(AppIdentityUser user, ICollection<string> newRoles);
    Task<UserResponse> SetUserPasswordAsync(AppIdentityUser user, string password);
    Task<UserResponse> CreateNewUser(AppIdentityUser newUser, string password, ICollection<string> roles = null);
    Task<ICollection<ApplicationRole>> GetRoles();
    Task AddUserToDefaultRoles(AppIdentityUser user);
}