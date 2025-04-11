using System.Collections.Generic;
using Damselfly.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.DbModels.Authentication;

public class AppIdentityUser : IdentityUser<int>, IDamselflyUser
{
    public ICollection<ApplicationUserRole> UserRoles { get; set; }

    /// <summary>
    /// The user's preferred Google Calendar ID for new events
    /// </summary>
    public string? PreferredCalendarId { get; set; }
}

public class ApplicationUserRole : IdentityUserRole<int>
{
    public virtual AppIdentityUser User { get; set; }
    public virtual ApplicationRole Role { get; set; }
}