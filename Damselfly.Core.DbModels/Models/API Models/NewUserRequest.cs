using System.Collections.Generic;
using Damselfly.Core.DbModels.Authentication;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class UserRequest
{
    public AppIdentityUser User { get; set; }
    public string Password { get; set; }
    public ICollection<string> Roles { get; set; }
}