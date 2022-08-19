using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class ClientFolderService : BaseClientService, IFolderService
{
    public ClientFolderService(HttpClient client, NotificationsService notificationService, ILogger<ClientFolderService> logger) : base(client)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    private readonly ILogger<ClientFolderService> _logger;
    private readonly NotificationsService _notificationService;
    public event Action OnChange;

    public async Task<ICollection<Folder>> GetFolders()
    {
        var folders = await httpClient.CustomGetFromJsonAsync<ICollection<Folder>>("/api/folders");
        return folders;
    }
}

