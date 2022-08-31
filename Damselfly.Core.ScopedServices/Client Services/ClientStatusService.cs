using System;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class ClientStatusService : IUserStatusService
{
    public event Action<string>? OnStatusChanged;
    private readonly IUserService _userService;
    private readonly NotificationsService _notifications;
    private readonly ILogger<ClientStatusService> _logger;
    private string statusText;

    private int? CurrentUserId => _userService.UserId;

    public ClientStatusService( NotificationsService notifications, IUserService userService, ILogger<ClientStatusService> logger )
    {
        _notifications = notifications;
        _userService = userService;
        _logger = logger;

        notifications.SubscribeToNotification<StatusUpdate>(Constants.NotificationType.StatusChanged, UpdateGlobalStatus);
    }

    private void NotifyStatusChanged( string newStatus )
    {
        _logger.LogInformation($"Status: {newStatus}");
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
        if (newStatus != statusText)
        {
            // This one is simple
            NotifyStatusChanged(newStatus);
        }
    }

    public void UpdateGlobalStatus(StatusUpdate newStatus)
    {
         if (newStatus.NewStatus != statusText)
        {
            // If it's -1, or it's meant for us, use it.
            if (! _userService.RolesEnabled || newStatus.UserID is null || newStatus.UserID == CurrentUserId )
            {
                NotifyStatusChanged(newStatus.NewStatus);
            }
        }
    }
}
