using System;
using System.Linq;
using Microsoft.AspNetCore.Components.Authorization;

namespace Damselfly.Core.Utils;

public static class AuthUtils
{
    public static int? GetUserIdFromPrincipal(this AuthenticationState authState)
    {
        try
        {
            var user = authState.User;

            if ( user != null && user.Identity != null && user.Identity.IsAuthenticated )
            {
                var userId = user.Claims
                    .Where(x => x.Type.Contains("NameIdentifier", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if ( userId != null )
                    if ( int.TryParse(userId.Value, out var id) )
                        return id;
            }
        }
        catch
        {
        }

        return null;
    }
}