using Damselfly.Core.Constants;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IWordpressService
{
    Task UploadImagesToWordpress(List<Image> images);
}

