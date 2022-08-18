using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace Damselfly.Core.ScopedServices;

public class ClientNotificationsService : IAsyncDisposable
{
    private readonly HubConnection hubConnection;

    public ClientNotificationsService()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:6363/notifications")
            .WithAutomaticReconnect()
            .Build();
       
        hubConnection.On<string, string>("Notify", (type, payload) =>
        {
            Console.WriteLine($"Received {type} message: {payload}");
        });

        _ = Task.Run(async () =>
        {
            await hubConnection.StartAsync();

            await Task.Delay(1000);
            Console.WriteLine($"Nofication hub onnected: {hubConnection.State} [ID: {hubConnection.ConnectionId}]");
        });
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (hubConnection is not null)
            await hubConnection.DisposeAsync();
    }
}

