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
using Accord.Imaging.Filters;

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
            Stopwatch watch = new Stopwatch("ControllerGetImage");
            IActionResult result = Redirect("/no-image.png");

            if (int.TryParse(imageId, out var id))
            {
                try
                {
                    var image = await ImageService.GetImage(id, false);

                    if (image != null)
                    {
                        var fs = System.IO.File.OpenRead(image.FullPath);
                        result = File(fs, "image/jpeg");
                    }
                }
                catch( Exception ex )
                {
                    Logging.LogError($"No thumb available for /rawmage/{imageId}: ", ex.Message);
                }
            }

            watch.Stop();

            return result;
        }

        [HttpGet("/thumb/{thumbSize}/{imageId}")]
        public async Task<IActionResult> Thumb(string thumbSize, string imageId, CancellationToken cancel)
        {
            Stopwatch watch = new Stopwatch("ControllerGetThumb");
            IActionResult result = Redirect("/no-image.png");

            if (Enum.TryParse<ThumbSize>( thumbSize, true, out var size) && int.TryParse(imageId, out var id))
            {
                try
                {
                    using var db = new ImageContext();
                    var image = SearchService.Instance.GetFromCache( id );

                    if (image == null)
                    {
                        Logging.Log($"Cache miss for image thumbnail: {id}");

                        image = await db.Images.Where(x => x.ImageId.Equals(id))
                                                    .Include(x => x.Folder)
                                                    .Include(x => x.MetaData)
                                                    .FirstOrDefaultAsync();
                    }

                    if (image != null)
                    {
                        var file = new FileInfo(image.FullPath);
                        var imagePath = ThumbnailService.Instance.GetThumbPath(file, size);
                        bool gotThumb = true;

                        if (! System.IO.File.Exists(imagePath))
                        {
                            gotThumb = false;
                            Logging.LogVerbose($"Generating thumbnail on-demand for {image.FileName}...");

                            var conversionResult = await ThumbnailService.Instance.ConvertFile(image, false, size);

                            if ( conversionResult.ThumbsGenerated )
                            {
                                gotThumb = true;

                                try
                                {
                                    if (image.MetaData != null)
                                    {
                                        db.Attach(image.MetaData);
                                        image.MetaData.Hash = conversionResult.ImageHash;
                                        image.MetaData.ThumbLastUpdated = DateTime.UtcNow;
                                        db.ImageMetaData.Update(image.MetaData);
                                    }
                                    else
                                    {
                                        var metadata = new ImageMetaData
                                        {
                                            ImageId = image.ImageId,
                                            Hash = conversionResult.ImageHash,
                                            ThumbLastUpdated = DateTime.UtcNow
                                        };
                                        db.ImageMetaData.Add(metadata);
                                        image.MetaData = metadata;
                                    }

                                    db.SaveChanges("ThumbUpdate");
                                }
                                catch (Exception ex)
                                {
                                    Logging.LogWarning($"Unable to update DB thumb for ID {imageId}: {ex.Message}");
                                }
                            }
                        }

                        if( gotThumb )
                        {
                            var fs = System.IO.File.OpenRead(imagePath);
                            result = File(fs, "image/jpeg");
                        }
                   }
                }
                catch (Exception ex)
                {
                    Logging.LogError($"Unable to process /thumb/{thumbSize}/{imageId}: ", ex.Message);
                }
            }

            watch.Stop();

            return result;
        }
    }
}