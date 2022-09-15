using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Damselfly.Shared.Utils;

public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public static string NotificationRoot => "notifications";

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