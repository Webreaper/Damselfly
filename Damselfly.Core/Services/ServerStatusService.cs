using System;
using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.Interfaces;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Services;

public class ServerStatusService : IStatusService
{
    public event Action<string> OnStatusChanged;
    private readonly ServerNotifierService _notifier;
    private readonly IUserService _userService;

    public ServerStatusService( ServerNotifierService notifier, IUserService userService)
    {
        _notifier = notifier;
        _userService = userService;
    }

    private int CurrentUserId => _userService.User == null ? -1 : _userService.User.Id;

    private void NotifyStateChanged(StatusUpdate update)
    {
        // UserID -1 means everyone should get it
        if ( update.UserID == -1 || update.UserID == CurrentUserId )
            OnStatusChanged?.Invoke(update.NewStatus);

        // Blazor WASM, we use the notify service and let the client handle it
        _ = _notifier.NotifyClients<StatusUpdate>(Constants.NotificationType.StatusChanged, update);
    }

    /// <summary>
    /// Sets the global application status. If a user is specified
    /// then the status will only be displayed for that user.
    /// </summary>
    /// <param name="newText"></param>
    /// <param name="user"></param>
    public void UpdateStatus(string newText)
    {
        NotifyStateChanged( new StatusUpdate {  NewStatus = newText, UserID = -1 });
    }

    /// <summary>
    /// Sets the global application status. If a user is specified
    /// then the status will only be displayed for that user.
    /// </summary>
    /// <param name="newText"></param>
    /// <param name="user"></param>
    public void UpdateUserStatus(string newText)
    {
        if (!_userService.RolesEnabled)
        {
            // Always notify if roles aren't enabled.
            NotifyStateChanged(new StatusUpdate { NewStatus = newText, UserID = -1 });
        }
        else
        {
            if (_userService.User != null)
            {
                // Only notify if the user is logged in
                NotifyStateChanged(new StatusUpdate { NewStatus = newText, UserID = CurrentUserId });
            }
        }
    }
}
