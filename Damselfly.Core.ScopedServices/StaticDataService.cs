using System;
namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Cached static data that the server knows, but the client needs to know
/// </summary>
public class StaticDataService
{
    private async Task InitialiseData()
    {
        // TODO: Make service call to initialise values
    }

    public string ImagesRootFolder { get; }
}

