using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IWordpressService
{
    Task UploadImagesToWordpress(List<Image> images);
}