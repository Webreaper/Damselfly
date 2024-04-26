using System.Collections.Generic;
using System.Linq;
using Damselfly.Core.DbModels.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class UserRequest
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
}

public class UserResponse
{
    public bool Succeeded { get; init; }
    public ICollection<string> Errors { get; init; }

    public UserResponse()   {

    }

    public UserResponse( IdentityResult result )
    {
        Succeeded = result.Succeeded;
        Errors = result.Errors.Select(x => x.Description).ToList();
    }
}