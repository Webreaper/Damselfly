using System;
using System.Collections.Generic;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class NewUserRequest
{
    public AppIdentityUser User { get; set; }
    public string Password { get; set; }
    public ICollection<string> Roles { get; set; }
}

