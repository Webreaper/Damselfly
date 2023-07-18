using System;
using System.Text.Json;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Shared.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.Services;

public class ServerNotifierService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<ServerNotifierService> _logger;

    public ServerNotifierService(IHubContext<NotificationHub> hubContext, ILogger<ServerNotifierService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyClients(NotificationType type, string? payloadMsg = null)
    {
        var methodName = type.ToString();

        if ( payloadMsg is null )
            payloadMsg = string.Empty;

        await _hubContext.Clients.All.SendAsync(methodName, payloadMsg);
    }

    public async Task NotifyClients<T>(NotificationType type, T payloadObject) where T : class
    {
        if ( payloadObject is null )
            throw new ArgumentException("Paylopad object cannot be null");

        var methodName = type.ToString();

        try
        {
            var json = JsonSerializer.Serialize(payloadObject, RestClient.JsonOptions);

            await _hubContext.Clients.All.SendAsync(methodName, json);
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Exception notifiying clients with method {methodName}: {ex}");
        }
    }
}