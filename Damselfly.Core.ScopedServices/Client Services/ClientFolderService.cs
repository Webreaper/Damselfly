﻿using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class ClientFolderService : IFolderService
{
    private readonly NotificationsService _notificationService;
    private readonly RestClient httpClient;
    protected ILogger<ClientFolderService> _logger;

    public ClientFolderService(RestClient client, NotificationsService notificationService,
        ILogger<ClientFolderService> logger)
    {
        httpClient = client;
        _logger = logger;
        _notificationService = notificationService;
    }

    public event Action OnChange;


    public async Task<ICollection<Folder>> GetFolders()
    {
        var folders = await httpClient.CustomGetFromJsonAsync<ICollection<Folder>>("/api/folders");
        return folders;
    }
}