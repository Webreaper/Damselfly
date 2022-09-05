using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.Services;
using Damselfly.Core.DbModels;
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
    private ImageCache imageCache;

    public ImageAPIController( ILogger<ImageAPIController> logger, ImageCache cache)
    {
        imageCache = cache;
        _logger = logger;
    }

    [HttpGet("/api/image/{imageId}")]
    public async Task<Image> Get(int imageId)
    {
        var image = await imageCache.GetCachedImage( imageId );

        return image;
    }

    [HttpPost("/api/images")]
    public async Task<ImageResponse> GetImages(ImageRequest req)
    {
        _logger.LogInformation($"Loading {req.ImageIds.Count} images from server cache.");
        var images = await imageCache.GetCachedImages(req.ImageIds);

        return new ImageResponse { Images = images };
    }
}