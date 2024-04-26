using System.Collections.Generic;
using System.Security.Claims;
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
    Task<UserResponse> UpdateUserAsync(string userName, string email, ICollection<string> newRoles);
    Task<UserResponse> SetUserPasswordAsync(string userName, string password);
    Task<UserResponse> CreateNewUser(string userName, string email, string password, ICollection<string>? roles = null);
    Task<ICollection<ApplicationRole>> GetRoles();
    Task AddUserToDefaultRoles(AppIdentityUser user);
    Task<AppIdentityUser> GetOrCreateUser(ClaimsPrincipal user);
}