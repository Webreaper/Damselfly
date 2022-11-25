using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.DbModels.Authentication;

public class ApplicationRole : IdentityRole<int>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName)
    {
        Name = roleName;
    }

    public ICollection<AppIdentityUser> AspNetUsers { get; set; }
    public ICollection<ApplicationUserRole> UserRoles { get; set; }
}