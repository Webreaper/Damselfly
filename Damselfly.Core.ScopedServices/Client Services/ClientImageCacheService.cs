using System;
using System.Net.Http;
using System.Net.Http.Json;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Cached static data that the server knows, but the client needs to know
/// </summary>
public class ClientImageCacheService : BaseClientService, IImageCacheService
{
    public ClientImageCacheService(HttpClient client) : base(client) { }

    public async Task<Image> GetCachedImage(int imgId)
    {
        return await httpClient.GetFromJsonAsync<Image>($"/api/image/{imgId}");
    }

    public async Task<List<Image>> GetCachedImages(ICollection<int> imgIds)
    {
        var response = await httpClient.PostAsJsonAsync<ICollection<int>>($"/api/images/", imgIds);

        return await response.Content.ReadFromJsonAsync<List<Image>>();
    }
}

