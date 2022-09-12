using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class ClientFolderService : IFolderService
{
    protected ILogger<ClientFolderService> _logger;
    private readonly RestClient httpClient;
    private readonly NotificationsService _notificationService;
    public event Action OnChange;

    public ClientFolderService(RestClient client, NotificationsService notificationService, ILogger<ClientFolderService> logger)
    {
        httpClient = client;
        _logger = logger;
        _notificationService = notificationService;
    }


    public async Task<ICollection<Folder>> GetFolders()
    {
        var folders = await httpClient.CustomGetFromJsonAsync<ICollection<Folder>>("/api/folders");
        return folders;
    }
}

