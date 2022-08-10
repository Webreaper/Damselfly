using System;
using System.Net.Http;
using System.Net.Http.Json;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Cached static data that the server knows, but the client needs to know
/// </summary>
public class CachedDataService : BaseClientService
{
    public CachedDataService(HttpClient client) : base(client) { }

    private async Task InitialiseData()
    {
        // WASM: AwaitALL
        await httpClient.GetFromJsonAsync<List<Camera>>($"/api/cameras");
        await httpClient.GetFromJsonAsync<List<Lens>>($"/api/lenses");
    }

    public string ImagesRootFolder { get; }

    public ICollection<Camera> Cameras { get; set; }
    public ICollection<Lens> Lenses { get; set; }
}

