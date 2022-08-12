using System;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Cached static data that the server knows, but the client needs to know
/// </summary>
public class CachedDataService
{
    private async Task InitialiseData()
    {
        // TODO: Make service call to initialise values
    }

    public string ImagesRootFolder { get; }

    public ICollection<Camera> Cameras { get;  }
    public ICollection<Lens> Lenses { get; }
}

