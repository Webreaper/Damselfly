using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Cached static data that the server knows, but the client needs to know
/// </summary>
public class ClientImageCacheService : BaseClientService, IImageCacheService
{
    public ClientImageCacheService(HttpClient client) : base(client) { }

    private static JsonSerializerOptions jsonOptions = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve };

    public async Task<Image> GetCachedImage(int imgId)
    {
        return await httpClient.CustomGetFromJsonAsync<Image>($"/api/image/{imgId}");
    }

    public async Task<List<Image>> GetCachedImages(ICollection<int> imgIds)
    {
        return await httpClient.CustomPutAsJsonAsync<ICollection<int>, List<Image>>($"/api/images/", imgIds);
    }
}

