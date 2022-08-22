using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface ICachedDataService
{
    Task InitialiseData();
    string ImagesRootFolder { get; }
    string ExifToolVer { get; }
    ICollection<Camera> Cameras { get; }
    ICollection<Lens> Lenses { get; }
}

