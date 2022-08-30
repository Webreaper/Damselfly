using System;
using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.Interfaces;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Services;

/// <summary>
/// Equivalent of the Client User Status Service, but for Blazor Server
/// </summary>
public class ServerUserStatusService : IUserStatusService
{
    public event Action<string> OnStatusChanged;
    private readonly IStatusService _statusService;
    private readonly IUserService _userService;

    public ServerUserStatusService(IStatusService serverStatus, IUserService userService)
    {
        _statusService = serverStatus;
        _userService = userService;
    }

    private int CurrentUserId => _userService.User == null ? -1 : _userService.User.Id;

    private void NotifyStateChanged(StatusUpdate update)
    {
        // UserID -1 means everyone should get it
        if (update.UserID == -1 || update.UserID == CurrentUserId)
        {
            OnStatusChanged?.Invoke(update.NewStatus);
        }
    }

    /// <summary>
    /// Sets the global application status. If a user is specified
    /// then the status will only be displayed for that user.
    /// </summary>
    /// <param name="newText"></param>
    /// <param name="user"></param>
    public void UpdateStatus(string newText)
    {
        NotifyStateChanged(new StatusUpdate { NewStatus = newText, UserID = CurrentUserId });
    }
}
