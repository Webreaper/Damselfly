using System;
using System.Net.Http;
using System.Net.Http.Json;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Cached static data that the server knows, but the client needs to know
/// </summary>
public class ClientDataService : ICachedDataService
{
    private readonly RestClient httpClient;
    private readonly List<Camera> _cameras = new List<Camera>();
    private readonly List<Lens> _lenses = new List<Lens>();

    public ClientDataService(RestClient client)
    {
        httpClient = client;
    }

    private async Task InitialiseData()
    {
        _cameras.Clear();
        _lenses.Clear();
        // WASM: AwaitALL
        _cameras.AddRange( await httpClient.CustomGetFromJsonAsync<List<Camera>>($"/api/cameras") );
        _lenses.AddRange( await httpClient.CustomGetFromJsonAsync<List<Lens>>($"/api/lenses") );
    }

    // WASM: TODO:
    public string ImagesRootFolder { get; }
    public string ExifToolVer { get; set; }
    public ICollection<Camera> Cameras => _cameras;
    public ICollection<Lens> Lenses => _lenses;
}

