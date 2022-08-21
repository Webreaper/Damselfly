using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.Services;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Damselfly.Core.Constants;
using Damselfly.Core.Interfaces;
using Damselfly.Shared.Utils;

namespace Damselfly.Web.Controllers;

/// <summary>
/// Image Controller used for dynamic as-loaded transforms
/// TODO: Convert this to use minimal APIs?
/// </summary>
[Route("/api/images")]
[ApiController]
public class ImageAPIController : ControllerBase
{
    private ILogger<ImageAPIController> _logger;

    public ImageAPIController( ILogger<ImageAPIController> logger )
    {
        _logger = logger;
    }

    [HttpGet("/api/image/{imageId}")]
    public async Task<Image> Get(int imageId, [FromServices] ImageCache imageCache)
    {
        return await imageCache.GetCachedImage( imageId );
    }

    [HttpPost("/api/images")]
    public async Task<List<Image>> GetImages(ICollection<int> images, [FromServices] ImageCache imageCache)
    {
        _logger.LogInformation($"Loading {images.Count} images from server cache.");
        return await imageCache.GetCachedImages(images);
    }
}