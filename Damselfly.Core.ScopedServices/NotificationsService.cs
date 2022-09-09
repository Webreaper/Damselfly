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
using Damselfly.Core.ScopedServices.ClientServices;

namespace Damselfly.Core.ScopedServices;

public class NotificationsService : IAsyncDisposable
{
    private class RetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay( RetryContext retryContext )
        {
            return TimeSpan.FromSeconds( 30 );
        }
    }

    private ILogger<NotificationsService> _logger;
    private readonly HubConnection hubConnection;
    private readonly WebAssemblyStatusService _wasmState;

    public event Action OnConnectionChanged;

    public NotificationsService(NavigationManager navManager, WebAssemblyStatusService wasmState, ILogger<NotificationsService> logger)
    {
        _logger = logger;
        _wasmState = wasmState;

        if (_wasmState.IsWebAssembly)
        {
            var hubUrl = $"{navManager.BaseUri}{NotificationHub.NotificationRoot}";
            _logger.LogInformation($"Setting up notifications listener on {hubUrl}...");

            hubConnection = new HubConnectionBuilder()
                            .WithUrl(hubUrl)
                            .WithAutomaticReconnect( new RetryPolicy() )
                            .Build();

            hubConnection.Closed += ConnectionClosed;
            hubConnection.Reconnected += ConnectionOpened;
            hubConnection.Reconnecting += ConnectionClosed;

            _ = Task.Run(async () =>
            {
                await hubConnection.StartAsync();
                OnConnectionChanged?.Invoke();
            } );
        }
        else
            _logger.LogInformation("Skipping notification service setup in Blazor Server mode.");
    }

    public HubConnectionState ConnectionState => hubConnection.State;

    private async Task ConnectionOpened( string? arg )
    {
        OnConnectionChanged?.Invoke();
    }

    private async Task ConnectionClosed( Exception? arg )
    {
        OnConnectionChanged?.Invoke();
    }

    /// <summary>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="action"></param>
    public void SubscribeToNotificationAsync<T>(NotificationType type, Func<T, Task> action)
    {
        if (action is null)
            throw new ArgumentException("Action cannot be null");

        if (!_wasmState.IsWebAssembly)
        {
            _logger.LogInformation($"Ignoring subscription to {type} in Blazor Server mode.");
            return;
        }

        var methodName = type.ToString();

        hubConnection.On<string>(methodName, async (payload) =>
        {
            try
            {
                T theObj = JsonSerializer.Deserialize<T>(payload, RestClient.JsonOptions);

                var payloadLog = string.IsNullOrEmpty(payload) ? "(no payload)" : $"(payload: {payload})";
                _logger.LogInformation($"Received {methodName.ToString()} - calling async action {payloadLog}");
                await action(theObj);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing serialized object for {methodName}: {payload}.");
            }
        });

        _logger.LogInformation($"Subscribed to {methodName}");
    }

    /// <summary>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="action"></param>
    public void SubscribeToNotification<T>(NotificationType type, Action<T> action)
    {
        if (action is null)
            throw new ArgumentException("Action cannot be null");

        if (!_wasmState.IsWebAssembly)
        {
            _logger.LogInformation($"Ignoring subscription to {type} in Blazor Server mode.");
            return;
        }

        var methodName = type.ToString();

        hubConnection.On<string>(methodName, (payload) =>
        {
            try
            {
                T theObj = JsonSerializer.Deserialize<T>(payload, RestClient.JsonOptions);

                var payloadLog = string.IsNullOrEmpty(payload) ? "(no payload)" : $"(payload: {payload})";
                _logger.LogInformation($"Received {methodName.ToString()} - calling action {payloadLog}");
                action.Invoke(theObj);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing serialized object for {methodName}: {payload}.");
            }
        });

        _logger.LogInformation($"Subscribed to {methodName}");
    }

    /// <summary>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="action"></param>
    public void SubscribeToNotification(NotificationType type, Action action)
    {
        if (!_wasmState.IsWebAssembly)
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
        if ( hubConnection is not null )
        {
            hubConnection.Closed -= ConnectionClosed;
            hubConnection.Reconnected -= ConnectionOpened;
            hubConnection.Reconnecting -= ConnectionClosed;
            await hubConnection.DisposeAsync();
        }
    }
}

