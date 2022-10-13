using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

/// <summary>
///     Cached static data that the server knows, but the client needs to know
/// </summary>
public class ClientDataService : ICachedDataService
{
    private readonly List<Camera> _cameras = new();
    private readonly List<Lens> _lenses = new();
    private readonly ILogger<ClientDataService> _logger;
    private readonly RestClient httpClient;
    private StaticData _staticData;

    public ClientDataService(RestClient client, ILogger<ClientDataService> logger)
    {
        httpClient = client;
        _logger = logger;
    }

    public string ImagesRootFolder => _staticData.ImagesRootFolder;
    public string ExifToolVer => _staticData.ExifToolVer;
    public ICollection<Camera> Cameras => _cameras;
    public ICollection<Lens> Lenses => _lenses;

    public async Task InitialiseData()
    {
        _cameras.Clear();
        _lenses.Clear();

        // Could do an await WhenAll
        _cameras.AddRange(await httpClient.CustomGetFromJsonAsync<List<Camera>>("/api/data/cameras"));
        _lenses.AddRange(await httpClient.CustomGetFromJsonAsync<List<Lens>>("/api/data/lenses"));

        _logger.LogTrace($"Loaded {_cameras.Count()} cameras, {_lenses.Count} lenses.");

        _staticData = await httpClient.CustomGetFromJsonAsync<StaticData>("/api/data/static");
    }

    public async Task<Statistics> GetStatistics()
    {
        return await httpClient.CustomGetFromJsonAsync<Statistics>("/api/data/stats");
    }
}