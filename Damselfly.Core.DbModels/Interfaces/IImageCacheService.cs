using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IImageCacheService
{
    Task<Image> GetCachedImage(int imgId);

    Task<List<Image>> GetCachedImages(ICollection<int> imgIds);
}