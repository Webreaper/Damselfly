using Microsoft.AspNetCore.Identity;
using Damselfly.Core.Interfaces;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System;

namespace Damselfly.Core.DbModels.Authentication
{
    public partial class AppIdentityUser : IdentityUser<int>, IDamselflyUser
    {
        public ICollection<ApplicationUserRole> UserRoles { get; set; }
    }

    public partial class ApplicationUserRole : IdentityUserRole<int>
    {
        public virtual AppIdentityUser User { get; set; }
        public virtual ApplicationRole Role { get; set; }
    }
}
