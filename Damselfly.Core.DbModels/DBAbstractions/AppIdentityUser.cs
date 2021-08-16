using Microsoft.AspNetCore.Identity;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.DbModels
{
    public class AppIdentityUser : IdentityUser<int>, IDamselflyUser
    {
    }
        
    public class ApplicationRole : IdentityRole<int>
    {
        public ApplicationRole() : base()
        {

        }

        public ApplicationRole(string roleName)
        {
            Name = roleName;
        }
    }
}
