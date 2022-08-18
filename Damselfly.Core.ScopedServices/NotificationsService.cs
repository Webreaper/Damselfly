using System;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Shared.Utils;
using Microsoft.AspNetCore.SignalR.Client;

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

            await Task.Delay(1000);
            Console.WriteLine($"Nofication hub connected: {hubConnection.State} [ID: {hubConnection.ConnectionId}]");

            hubConnection.On( "NotifyFolderChanged", () => { Console.WriteLine($"Received FolderChangewd"); });
        });
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (hubConnection is not null)
            await hubConnection.DisposeAsync();
    }
}

