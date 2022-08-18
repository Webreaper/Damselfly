using System;
using System.Threading.Tasks;
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

    public async Task NotifyClients( string message )
    {
        await _hubContext.Clients.All.SendAsync(message);
    }
}

