using System;
using System.Text.RegularExpressions;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Shared.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace Damselfly.Core.ScopedServices;

public class NotificationsService : IAsyncDisposable
{
    private readonly HubConnection hubConnection;

    public NotificationsService()
    {
        hubConnection = new HubConnectionBuilder()
                        .WithUrl($"http://localhost:6363{NotificationHub.NotificationRoot}")
                        .WithAutomaticReconnect()
                        .Build();

        _ = Task.Run(async () =>
        {
            await hubConnection.StartAsync();
        });
    }
    public void SubscribeToNotification( string notificationType, Action action)
    {
        hubConnection.On(notificationType, () =>
        {
            Console.WriteLine($"Received {notificationType} - calling action.");
            action.Invoke();
            // WASM: TODO: Unsubscribe and decompose
        });

        Console.WriteLine($"Subscribed to {notificationType}");
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (hubConnection is not null)
            await hubConnection.DisposeAsync();
    }
}

