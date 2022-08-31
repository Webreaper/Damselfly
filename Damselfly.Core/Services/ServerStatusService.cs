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

    public ServerStatusService( ServerNotifierService notifier)
    {
        _notifier = notifier;
    }

    internal void NotifyStateChanged(StatusUpdate update)
    {
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
    public void UpdateStatus(string newText, int? userId = null )
    {
        NotifyStateChanged( new StatusUpdate {  NewStatus = newText, UserID = userId });
    }
}
