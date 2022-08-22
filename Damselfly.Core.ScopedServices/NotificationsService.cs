using System;
using System.Text.RegularExpressions;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Shared.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Damselfly.Core.ScopedServices;

public class NotificationsService : IAsyncDisposable
{
    private ILogger<NotificationsService> _logger;
    private readonly HubConnection hubConnection;
    private bool isWebAssembly { get; }

    public NotificationsService( NavigationManager navManager, IJSRuntime jsRuntime, ILogger<NotificationsService> logger )
    {
        _logger = logger;
        isWebAssembly = jsRuntime is IJSInProcessRuntime;

        if (isWebAssembly)
        {
            var hubUrl = $"{navManager.BaseUri}{NotificationHub.NotificationRoot}";
            _logger.LogInformation($"Setting up notifications listener on {hubUrl}...");

            hubConnection = new HubConnectionBuilder()
                            .WithUrl(hubUrl)
                            .WithAutomaticReconnect()
                            .Build();

            _ = Task.Run(async () =>
            {
                await hubConnection.StartAsync();
            });
        }
        else
            _logger.LogInformation("Skipping notification service setup in Blazor Server mode.");
    }

    /// <summary>
    /// WASM: TODO: Unsubscribe and decompose
    /// </summary>
    /// <param name="type"></param>
    /// <param name="action"></param>
    public void SubscribeToNotification<T>(NotificationType type, Action<T> action)
    {
        if (action is null)
            throw new ArgumentException("Action cannot be null");

        if (!isWebAssembly)
        {
            _logger.LogInformation($"Ignoring subscription to {type} in Blazor Server mode.");
            return;
        }

        var methodName = type.ToString();

        hubConnection.On<string>(methodName, (payload) =>
        {
            try
            {
                T theObj = JsonSerializer.Deserialize<T>(payload);

                var payloadLog = string.IsNullOrEmpty(payload) ? "(no payload)" : $"(payload: {payload})";
                _logger.LogInformation($"Received {methodName.ToString()} - calling action {payloadLog}");
                action.Invoke(theObj);
            }
            catch( Exception ex )
            {
                _logger.LogError( $"Error processing serialized object for {methodName}: {payload}.");
            }
        });

        _logger.LogInformation($"Subscribed to {methodName}");
    }

    /// <summary>
    /// WASM: TODO: Unsubscribe and decompose
    /// </summary>
    /// <param name="type"></param>
    /// <param name="action"></param>
    public void SubscribeToNotification(NotificationType type, Action action)
    {
        if (!isWebAssembly)
        {
            _logger.LogInformation($"Ignoring subscription to {type} in Blazor Server mode.");
            return;
        }

        var methodName = type.ToString();

        hubConnection.On<string>(methodName, (payload) =>
        {
            var payloadLog = string.IsNullOrEmpty(payload) ? "(no payload)" : $"(payload: {payload})";
            _logger.LogInformation($"Received {methodName.ToString()} - calling action {payloadLog}");
            action.Invoke();
        });

        _logger.LogInformation($"Subscribed to {methodName}");
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (hubConnection is not null)
            await hubConnection.DisposeAsync();
    }
}

