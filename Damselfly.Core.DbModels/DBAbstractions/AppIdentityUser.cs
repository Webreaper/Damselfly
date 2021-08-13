using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.DbModels
{
    public class AppIdentityUser : IdentityUser<int>
    {
    }
        
    public class ApplicationRole : IdentityRole<int>
    {
    }
}
