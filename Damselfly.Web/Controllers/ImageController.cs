using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Services;
using Damselfly.Web.Data;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
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
        [HttpGet("/dlimage/{imageId}")]
        public async Task<IActionResult> Image(string imageId, CancellationToken cancel, [FromServices] SearchService searchService)
        {
            return await Image(imageId, cancel, searchService, true);
        }

        [HttpGet("/rawimage/{imageId}")]
        public async Task<IActionResult> Image(string imageId, CancellationToken cancel, [FromServices] SearchService searchService, bool isDownload = false )
        {
            Stopwatch watch = new Stopwatch("ControllerGetImage");

            IActionResult result = Redirect("/no-image.png");

            if (int.TryParse(imageId, out var id))
            {
                try
                {
                    var image = searchService.GetFromCache(id);

                    if (cancel.IsCancellationRequested)
                        return result;

                    if (image == null)
                    {
                        image = await ImageService.GetImage(id, false, false);
                    }

                    if (image != null)
                    {
                        string downloadFilename = null;

                        if (isDownload)
                            downloadFilename = image.FileName;

                        if (cancel.IsCancellationRequested)
                            return result;

                        result = PhysicalFile(image.FullPath, "image/jpeg", downloadFilename);
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
        public async Task<IActionResult> Thumb(string thumbSize, string imageId, CancellationToken cancel,
                        [FromServices] SearchService searchService, [FromServices] ThumbnailService thumbService)
        {
            Stopwatch watch = new Stopwatch("ControllerGetThumb");

            IActionResult result = Redirect("/no-image.png");

            if (Enum.TryParse<ThumbSize>( thumbSize, true, out var size) && int.TryParse(imageId, out var id))
            {
                try
                {
                    Logging.LogTrace($"Controller - Getting Thumb for {imageId}");

                    using var db = new ImageContext();
                    var image = searchService.GetFromCache( id );

                    if (cancel.IsCancellationRequested)
                        return result;

                    if (image == null)
                    {
                        Logging.LogTrace($" - Cache miss for image thumbnail: {id}");

                        image = await ImageService.GetImage(id, true, false);
                    }

                    if (image != null)
                    {
                        if (cancel.IsCancellationRequested)
                            return result;

                        Logging.LogTrace($" - Getting thumb path for {imageId}");

                        var file = new FileInfo(image.FullPath);
                        var imagePath = thumbService.GetThumbPath(file, size);
                        bool gotThumb = true;


                        if (! System.IO.File.Exists(imagePath))
                        {
                            gotThumb = false;
                            Logging.LogTrace($" - Generating thumbnail on-demand for {image.FileName}...");

                            if (cancel.IsCancellationRequested)
                                return result;

                            var conversionResult = await thumbService.ConvertFile(image, false, size);

                            if ( conversionResult.ThumbsGenerated )
                            {
                                gotThumb = true;

                                Logging.LogTrace($" - Updating metadata for {imageId}");
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

                                    await db.SaveChangesAsync("ThumbUpdate");
                                }
                                catch (Exception ex)
                                {
                                    Logging.LogWarning($"Unable to update DB thumb for ID {imageId}: {ex.Message}");
                                }
                            }
                        }

                        if (cancel.IsCancellationRequested)
                            return result;

                        if ( gotThumb )
                        {
                            Logging.LogTrace($" - Loading file for {imageId}");

                            result = PhysicalFile(imagePath, "image/jpeg");
                        }

                        Logging.LogTrace($"Controller - served thumb for {imageId}");
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