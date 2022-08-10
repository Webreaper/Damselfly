using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;

namespace Damselfly.Core.ScopedServices;

public class ClientWordpressService : BaseClientService
{
    public ClientWordpressService(HttpClient client) : base(client) { }

    public async Task<HttpResponseMessage> UploadImagesToWordpress(List<Image> images)
    {
        return await httpClient.PostAsJsonAsync("/api/wordpress", images);
    }
}

