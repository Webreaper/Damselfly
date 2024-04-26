using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[Route("/api/images")]
[ApiController]
[AuthorizeFireBase(RoleDefinitions.s_AdminRole)]
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
            // This is a bit unwieldy. Because of the problems serializing large and complicated sets of DF
            // entities, we can't just make a single request to the DB, serialize them all, and then send 
            // the result to the client - because the nested/cyclic values get in a right state. So, to work
            // around this, we request each image from the cache individually, and then add it to the list
            // and send the list back. However, calling GetCachedImage 250 times could potentially result 
            // in 250 SQL calls, which is going to be horribly slow. So, we:
            //   1. Call GetCachedImages with the full list, which will hit the DB once and cache the results
            //   2. Discard the result from that call
            //   3. Then call GetCachedImage individually for each image, which won't hit the DB as
            //      the image will already be in the cache at this point
            //   4. We then assemble the individual images into list and return it in the response
            // Note that technically, the WhenAll results could be returned in any order, but we don't care
            // because the client code in the ClientImageCache merely uses thes results to populate the
            // client-side cache. It then assembles the list in-order from the client-side cache so that the
            // UI has the images in the right order....
            await imageCache.GetCachedImages(req.ImageIds);

            var imageTasks = req.ImageIds.Select(async x => await imageCache.GetCachedImage(x));

            var images = await Task.WhenAll(imageTasks);

            var response = new ImageResponse { Images = images.ToList() };
            return response;
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Unable to load/enrich images: {string.Join(", ", req.ImageIds)}: {ex}");
        }

        return null;
    }
}