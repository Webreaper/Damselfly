using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Controllers;

/// <summary>
///     Image Controller used for dynamic as-loaded transforms
///     TODO: Convert this to use minimal APIs?
/// </summary>
[Route("/api/images")]
[ApiController]
public class ImageAPIController : ControllerBase
{
    private readonly ILogger<ImageAPIController> _logger;
    private readonly ImageCache imageCache;

    public ImageAPIController(ILogger<ImageAPIController> logger, ImageCache cache)
    {
        imageCache = cache;
        _logger = logger;
    }

    [HttpGet("/api/image/{imageId}")]
    public async Task<Image> Get(int imageId)
    {
        try
        {
            return await imageCache.GetCachedImage(imageId);
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Unable to load/enrich image ID {imageId}: {ex}");
            return null;
        }
    }

    [HttpPost("/api/images/batch")]
    public async Task<ImageResponse> GetImages(ImageRequest req)
    {
        try
        {
            var images = await imageCache.GetCachedImages(req.ImageIds);
            var response = new ImageResponse { Images = images };
            return response;
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Unable to load/enrich images: {string.Join(", ", req.ImageIds)}: {ex}");
        }

        return null;
    }
}