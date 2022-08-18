using System;
using Microsoft.AspNetCore.SignalR;

namespace Damselfly.Shared.Utils;

public class NotificationHub : Hub
{
    public async Task SendMessage(string type, string payload)
    {
        await Clients.All.SendAsync("Notify", type, payload);
    }
}

