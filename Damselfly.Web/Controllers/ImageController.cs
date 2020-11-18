using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Services;
using Damselfly.Web.Data;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace Damselfly.Web.Controllers
{
    [Produces("image/jpeg")]
    [Route("images")]
    [ApiController]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Controller methods cannot be static")]
    public class ImageController : Controller
    {   
        [HttpGet("/rawimage/{imageId}")]
        [ResponseCache(CacheProfile = "Test")]
        public async Task<IActionResult> Image(string imageId, CancellationToken cancel)
        {
            if (int.TryParse(imageId, out var id))
            {
                try
                {
                    var image = await ImageService.GetImage(id, false);

                    if (image != null)
                    {
                        var stream = new FileStream(image.FullPath, FileMode.Open);
                        var result = new FileStreamResult(stream, "image/jpeg");
                        result.FileDownloadName = image.FileName;
                        return result;
                    }
                }
                catch( Exception ex )
                {
                    Logging.LogError($"Unable to process /rawmage/{imageId}: ", ex.Message);
                }
            }

            return null;
        }

        [HttpGet("/thumb/{thumbSize}/{imageId}")]
        public async Task<IActionResult> Thumb(string thumbSize, string imageId, CancellationToken cancel)
        {
            if (Enum.TryParse<ThumbSize>( thumbSize, true, out var size) && int.TryParse(imageId, out var id))
            {
                try
                {
                    var image = await ImageService.GetImage(id, false);

                    if (image != null)
                    {
                        var file = new FileInfo(image.FullPath);
                        var path = ThumbnailService.Instance.GetThumbPath(file, size);

                        if (!System.IO.File.Exists(path))
                            path = "/no-image.png";

                        var stream = new FileStream(path, FileMode.Open);
                        var result = new FileStreamResult(stream, "image/jpeg")
                        {
                            FileDownloadName = image.FileName
                        };
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogError($"Unable to process /thumb/{thumbSize}/{imageId}: ", ex.Message);
                }
            }

            return null;
        }
    }
}