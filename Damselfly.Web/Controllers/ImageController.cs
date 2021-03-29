using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Services;
using Damselfly.Web.Data;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;

namespace Damselfly.Web.Controllers
{
    [Produces("image/jpeg")]
    [Route("images")]
    [ApiController]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Controller methods cannot be static")]
    public class ImageController : Controller
    {   
        [HttpGet("/rawimage/{imageId}")]
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
            Stopwatch watch = new Stopwatch("ControllerGetThumb");
            IActionResult result = Redirect("/no-image.png");
            string imagePath = null;

            if (Enum.TryParse<ThumbSize>( thumbSize, true, out var size) && int.TryParse(imageId, out var id))
            {
                try
                {
                    using var db = new ImageContext();
                    var image = SearchService.Instance.GetFromCache( id );

                    if (image == null)
                    {
                        image = await db.Images.Where(x => x.ImageId.Equals(id))
                                                    .Include(x => x.Folder)
                                                    .Include(x => x.MetaData)
                                                    .FirstOrDefaultAsync();
                    }

                    if (image != null)
                    {
                        var file = new FileInfo(image.FullPath);
                        var path = ThumbnailService.Instance.GetThumbPath(file, size);

                        if (System.IO.File.Exists(path))
                        {
                            imagePath = path;
                        }
                        else
                        {
                            Logging.Log($"Generating thumbnail on-demand for {image.FileName}...");
                            if( ThumbnailService.Instance.ConvertFile(image, false, out var hash) )
                            {
                                image.MetaData.Hash = hash;
                                image.MetaData.ThumbLastUpdated = DateTime.UtcNow;
                                db.Attach(image);
                                db.Update(image.MetaData);
                                db.SaveChanges("ThumbUpdate");

                                imagePath = ThumbnailService.Instance.GetThumbPath(file, size);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogError($"Unable to process /thumb/{thumbSize}/{imageId}: ", ex.Message);
                }
            }

            if (!string.IsNullOrEmpty(imagePath))
            {
                var stream = new FileStream(imagePath, FileMode.Open);
                result = new FileStreamResult(stream, "image/jpeg")
                {
                    FileDownloadName = imagePath
                };
            }

            watch.Stop();

            return result;
        }
    }
}