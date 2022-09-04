using System;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Shared.Utils;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Damselfly.Core.ScopedServices.ClientServices;

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

    public async Task NotifyClients( NotificationType type, string payloadMsg = null )
    {
        string methodName = type.ToString();

        if (payloadMsg is null)
            payloadMsg = string.Empty;

        await _hubContext.Clients.All.SendAsync(methodName, payloadMsg);
    }

    public async Task NotifyClients<T>(NotificationType type, T payloadObject) where T : class
    {
        if (payloadObject is null)
            throw new ArgumentException("Paylopad object cannot be null");

        string methodName = type.ToString();

        try
        {
            string json = JsonSerializer.Serialize(payloadObject, RestClient.JsonOptions);

            await _hubContext.Clients.All.SendAsync(methodName, json);
        }
        catch( Exception ex )
        {
            _logger.LogError($"Exception notifiying clients with method {methodName}: {ex}");
        }
    }
}

