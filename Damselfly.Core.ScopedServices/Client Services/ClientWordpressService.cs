using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.ScopedServices.ClientServices;

namespace Damselfly.Core.ScopedServices;

public class ClientWordpressService : IWordpressService
{
    private readonly RestClient httpClient;

    public ClientWordpressService(RestClient client)
    {
        httpClient = client;
    }

    public async Task UploadImagesToWordpress(List<Image> images)
    {
        await httpClient.CustomPostAsJsonAsync("/api/wordpress", images);
    }
}

