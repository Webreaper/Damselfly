using System;
using Microsoft.AspNetCore.SignalR;

namespace Damselfly.Shared.Utils;

public class NotificationHub : Hub
{
    public static string NotificationRoot => "notifications";

    public async Task SendMessage(string type, string payload)
    {
        await Clients.All.SendAsync("Notify", type, payload);
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("Notify Service connected.");
        await base.OnConnectedAsync();
    }


}

