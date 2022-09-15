using System;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.Services;

public class ServerStatusService : IStatusService
{
    private readonly ServerNotifierService _notifier;

    public ServerStatusService(ServerNotifierService notifier)
    {
        _notifier = notifier;
    }

    public event Action<string> OnStatusChanged;

    /// <summary>
    ///     Sets the global application status. If a user is specified
    ///     then the status will only be displayed for that user.
    /// </summary>
    /// <param name="newText"></param>
    /// <param name="user"></param>
    public void UpdateStatus(string newText, int? userId = null)
    {
        NotifyStateChanged(new StatusUpdate { NewStatus = newText, UserID = userId });
    }

    internal void NotifyStateChanged(StatusUpdate update)
    {
        OnStatusChanged?.Invoke(update.NewStatus);

        // Blazor WASM, we use the notify service and let the client handle it
        _ = _notifier.NotifyClients(NotificationType.StatusChanged, update);
    }
}