using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class ClientStatusService : IUserStatusService
{
    private readonly ILogger<ClientStatusService> _logger;
    private readonly NotificationsService _notifications;
    private readonly RestClient _restClient;
    private readonly IUserService _userService;

    public ClientStatusService(NotificationsService notifications, RestClient restClient, IUserService userService,
        ILogger<ClientStatusService> logger)
    {
        _restClient = restClient;
        _notifications = notifications;
        _userService = userService;
        _logger = logger;

        _notifications.SubscribeToNotification<StatusUpdate>(NotificationType.StatusChanged, ShowServerStatus);
    }

    private int? CurrentUserId => _userService.UserId;

    public string StatusText { get; private set; }

    public event Action<string>? OnStatusChanged;

    public void UpdateStatus(string newStatus)
    {
        NotifyStatusChanged(newStatus);
    }

    private void NotifyStatusChanged(string newStatus)
    {
        if ( StatusText != newStatus )
        {
            _logger.LogInformation($"Status: {newStatus}");
            StatusText = newStatus;
            OnStatusChanged?.Invoke(newStatus);
        }
    }

    public void UpdateGlobalStatus(string newStatus)
    {
        var payload = new StatusUpdateRequest { NewStatus = newStatus, UserId = null };
        _ = _restClient.CustomPostAsJsonAsync("/api/status", payload);
    }

    private void ShowServerStatus(StatusUpdate newStatus)
    {
        // If it's -1, or it's meant for us, use it.
        if ( !_userService.RolesEnabled || newStatus.UserID is null || newStatus.UserID == CurrentUserId )
            NotifyStatusChanged(newStatus.NewStatus);
    }
}