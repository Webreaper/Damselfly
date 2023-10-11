using System.Text.Json;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Shared.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class NotificationsService : IAsyncDisposable
{
    private readonly ILogger<NotificationsService> _logger;
    private readonly ApplicationStateService _appState;
    private readonly HubConnection hubConnection;

    public NotificationsService(NavigationManager navManager, ApplicationStateService appState,
        ILogger<NotificationsService> logger)
    {
        _logger = logger;
        _appState = appState;

        if (_appState.IsWebAssembly )
        {
            var hubUrl = $"{navManager.BaseUri}{NotificationHub.NotificationRoot}";
            _logger.LogInformation($"Setting up notifications listener on {hubUrl}...");

            hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => { 
                    options.UseStatefulReconnect = true; 
                } )
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();

            hubConnection.Closed += ConnectionClosed;
            hubConnection.Reconnected += ConnectionOpened;
            hubConnection.Reconnecting += ConnectionClosed;

            _ = Task.Run(async () =>
            {
                await hubConnection.StartAsync();
                OnConnectionChanged?.Invoke();
            });
        }
        else
        {
            _logger.LogInformation("Skipping notification service setup in Blazor Server mode.");
        }
    }

    public HubConnectionState ConnectionState => hubConnection.State;

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

    public event Action OnConnectionChanged;

    private Task ConnectionOpened(string? arg)
    {
        OnConnectionChanged?.Invoke();
        return Task.CompletedTask;
    }

    private Task ConnectionClosed(Exception? arg)
    {
        OnConnectionChanged?.Invoke();
        return Task.CompletedTask;
    }

    /// <summary>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="action"></param>
    public void SubscribeToNotificationAsync<T>(NotificationType type, Func<T, Task> action)
    {
        if ( action is null )
            throw new ArgumentException("Action cannot be null");

        if ( !_appState.IsWebAssembly )
        {
            _logger.LogInformation($"Ignoring subscription to {type} in Blazor Server mode.");
            return;
        }

        var methodName = type.ToString();

        hubConnection.On<string>(methodName, async payload =>
        {
            try
            {
                var theObj = JsonSerializer.Deserialize<T>(payload, RestClient.JsonOptions);

                var payloadLog = string.IsNullOrEmpty(payload) ? "(no payload)" : $"(payload: {payload})";
                _logger.LogInformation($"Received {methodName} - calling async action {payloadLog}");
                await action(theObj);
            }
            catch ( Exception )
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
        if ( action is null )
            throw new ArgumentException("Action cannot be null");

        if ( !_appState.IsWebAssembly )
        {
            _logger.LogInformation($"Ignoring subscription to {type} in Blazor Server mode.");
            return;
        }

        var methodName = type.ToString();

        hubConnection.On<string>(methodName, payload =>
        {
            try
            {
                var theObj = JsonSerializer.Deserialize<T>(payload, RestClient.JsonOptions);

                var payloadLog = string.IsNullOrEmpty(payload) ? "(no payload)" : $"(payload: {payload})";
                _logger.LogInformation($"Received {methodName} - calling action {payloadLog}");
                action.Invoke(theObj);
            }
            catch ( Exception )
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
        if ( !_appState.IsWebAssembly )
        {
            _logger.LogInformation($"Ignoring subscription to {type} in Blazor Server mode.");
            return;
        }

        var methodName = type.ToString();

        hubConnection.On<string>(methodName, payload =>
        {
            var payloadLog = string.IsNullOrEmpty(payload) ? "(no payload)" : $"(payload: {payload})";
            _logger.LogInformation($"Received {methodName} - calling action {payloadLog}");
            action.Invoke();
        });

        _logger.LogInformation($"Subscribed to {methodName}");
    }

    private class RetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return TimeSpan.FromSeconds(30);
        }
    }
}