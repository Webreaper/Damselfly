using System;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels;
using Microsoft.Extensions.Logging;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.DbModels.Models.APIModels;

namespace Damselfly.Core.ScopedServices;

public class ClientStatusService : IUserStatusService
{
    public event Action<string>? OnStatusChanged;
    private readonly IUserService _userService;
    private readonly NotificationsService _notifications;
    private readonly ILogger<ClientStatusService> _logger;
    private readonly RestClient _restClient;
    private string statusText;

    private int? CurrentUserId => _userService.UserId;

    public ClientStatusService(NotificationsService notifications, RestClient restClient, IUserService userService, ILogger<ClientStatusService> logger)
    {
        _restClient = restClient;
        _notifications = notifications;
        _userService = userService;
        _logger = logger;

        _notifications.SubscribeToNotification<StatusUpdate>(Constants.NotificationType.StatusChanged, ShowServerStatus);
    }

    private void NotifyStatusChanged(string newStatus)
    {
        if (statusText != newStatus)
        {

            _logger.LogInformation($"Status: {newStatus}");
            statusText = newStatus;
            OnStatusChanged?.Invoke(newStatus);
        }
    }

    public string StatusText { get { return statusText; } }

    public void UpdateStatus(string newStatus)
    {
        NotifyStatusChanged(newStatus);
    }

    public void UpdateGlobalStatus(string newStatus)
    {
        var payload = new StatusUpdateRequest { NewStatus = newStatus, UserId = null };
        _ = _restClient.CustomPostAsJsonAsync("/api/status", payload);
    }

    private void ShowServerStatus(StatusUpdate newStatus)
    {
        // If it's -1, or it's meant for us, use it.
        if (!_userService.RolesEnabled || newStatus.UserID is null || newStatus.UserID == CurrentUserId)
        {
            NotifyStatusChanged(newStatus.NewStatus);
        }
    }
}
