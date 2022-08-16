using System;
using Damselfly.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IImageCacheService
{
    Task<Image> GetCachedImage(int imgId);
}

