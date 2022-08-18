using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class ClientWordpressService : BaseClientService, IWordpressService
{
    public ClientWordpressService(HttpClient client) : base(client) { }

    public async Task UploadImagesToWordpress(List<Image> images)
    {
        await httpClient.PostAsJsonAsync("/api/wordpress", images);
    }
}

