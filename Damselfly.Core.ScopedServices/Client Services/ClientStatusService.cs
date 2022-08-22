using System;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels;

namespace Damselfly.Core.ScopedServices;

public class ClientStatusService : IUserStatusService
{
    public event Action<string>? OnStatusChanged;
    private readonly IUserService _userService;
    private readonly NotificationsService _notifications;
    private string statusText;

    private int CurrentUserId => _userService.User == null ? -1 : _userService.User.Id;

    public ClientStatusService( NotificationsService notifications, IUserService userService )
    {
        _notifications = notifications;
        _userService = userService;

        notifications.SubscribeToNotification<StatusUpdate>(Constants.NotificationType.StatusChanged, UpdateGlobalStatus);
    }

    private void NotifyStatusChanged( string newStatus )
    {
        statusText = newStatus;
        OnStatusChanged?.Invoke(newStatus);
    }

    public string StatusText {  get { return statusText;  } }

    public void UpdateStatus(string newStatus)
    {
        NotifyStatusChanged(newStatus);
    }

    public void UpdateUserStatus(string newStatus)
    {
        if( _userService.User != null )
        {
            var update = new StatusUpdate { NewStatus = newStatus, UserID = CurrentUserId };

            UpdateGlobalStatus(update);
        }
    }

    public void UpdateGlobalStatus(StatusUpdate newStatus)
    {
        if (newStatus.NewStatus != statusText)
        {
            // If it's -1, or it's meant for us, use it.
            if (newStatus.UserID == -1 || newStatus.UserID == CurrentUserId )
            {
                NotifyStatusChanged(newStatus.NewStatus);
            }
        }
    }
}
