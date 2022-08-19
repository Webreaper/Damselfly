using System;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Shared.Utils;
using Microsoft.AspNetCore.SignalR;

namespace Damselfly.Core.Services;

public class ServerNotifierService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public ServerNotifierService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyClients( NotificationType type, string payloadMsg = null )
    {
        string methodName = type.ToString();

        if (payloadMsg is null)
            payloadMsg = string.Empty;

        await _hubContext.Clients.All.SendAsync(methodName, payloadMsg);
    }
}

