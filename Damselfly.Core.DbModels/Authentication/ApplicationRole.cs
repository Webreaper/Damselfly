using System;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;

namespace Damselfly.Core.DbModels.Authentication;


public partial class ApplicationRole : IdentityRole<int>
{
    public ApplicationRole() : base()
    {

    }

    public ApplicationRole(string roleName)
    {
        Name = roleName;
    }

    public ICollection<AppIdentityUser> AspNetUsers { get; set; }
    public ICollection<ApplicationUserRole> UserRoles { get; set; }
}
