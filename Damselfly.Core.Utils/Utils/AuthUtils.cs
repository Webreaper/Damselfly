using System;
using Microsoft.AspNetCore.Components.Authorization;

namespace Damselfly.Core.Utils;

public static class AuthUtils
{
    public static int? GetUserIdFromPrincipal( this AuthenticationState authState )
    {
        try
        {
            if ( authState.User.Identity.IsAuthenticated )
            {
                var userId = authState.User.FindFirst( c => c.Type == "sub" )?.Value;

                if ( int.TryParse( userId, out var id ) )
                {
                    return id;
                }
            }
        }
        catch { }
        return null;
    }
}

