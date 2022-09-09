using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Damselfly.Shared.Utils;

public class NotificationHub : Hub
{
    private ILogger<NotificationHub> _logger;
    public static string NotificationRoot => "notifications";

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public async Task SendMessage(string type, string payload)
    {
        await Clients.All.SendAsync("Notify", type, payload);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogTrace("Notify Hub connected.");
        await base.OnConnectedAsync();
    }
}

