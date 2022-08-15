using System;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces
{
    public interface ICachedDataService
    {
        string ImagesRootFolder { get; }
        string ExifToolVer { get; }
        ICollection<Camera> Cameras { get; }
        ICollection<Lens> Lenses { get; }
    }
}

