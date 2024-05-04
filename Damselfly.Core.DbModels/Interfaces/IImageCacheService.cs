using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IImageCacheService
{
    Task<Image> GetCachedImage(System.Guid imgId);
    Task<List<Image>> GetCachedImages(ICollection<System.Guid> imgIds);
    Task ClearCache();
}