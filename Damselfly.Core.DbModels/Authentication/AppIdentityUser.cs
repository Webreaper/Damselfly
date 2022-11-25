using System.Collections.Generic;
using Damselfly.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.DbModels.Authentication;

public class AppIdentityUser : IdentityUser<int>, IDamselflyUser
{
    public ICollection<ApplicationUserRole> UserRoles { get; set; }
}

public class ApplicationUserRole : IdentityUserRole<int>
{
    public virtual AppIdentityUser User { get; set; }
    public virtual ApplicationRole Role { get; set; }
}