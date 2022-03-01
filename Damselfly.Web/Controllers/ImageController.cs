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
using Damselfly.Core.Utils.Images;

namespace Damselfly.Web.Controllers
{
    [Produces("image/jpeg")]
    [Route("images")]
    [ApiController]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Controller methods cannot be static")]
    public class ImageController : Controller
    {
        [HttpGet("/dlimage/{imageId}")]
        public async Task<IActionResult> Image(string imageId, CancellationToken cancel, [FromServices] ImageCache imageCache)
        {
            return await Image(imageId, cancel, imageCache, true);
        }

        [HttpGet("/rawimage/{imageId}")]
        public async Task<IActionResult> Image(string imageId, CancellationToken cancel, [FromServices] ImageCache imageCache, bool isDownload = false )
        {
            Stopwatch watch = new Stopwatch("ControllerGetImage");

            IActionResult result = Redirect("/no-image.png");

            if (int.TryParse(imageId, out var id))
            {
                try
                {
                    var image = await imageCache.GetCachedImage(id);

                    if (cancel.IsCancellationRequested)
                        return result;

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
                        [FromServices] ImageCache imageCache, [FromServices] ThumbnailService thumbService)
        {
            Stopwatch watch = new Stopwatch("ControllerGetThumb");

            IActionResult result = Redirect("/no-image.png");

            if (Enum.TryParse<ThumbSize>( thumbSize, true, out var size) && int.TryParse(imageId, out var id))
            {
                try
                {
                    Logging.LogTrace($"Controller - Getting Thumb for {imageId}");

                    var image = await imageCache.GetCachedImage(id);

                    if (cancel.IsCancellationRequested)
                        return result;

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

                                // TODO: Do we do this here? If we don't gen all the thumbs, it'll end up
                                // with stuff like AI not working later.
                                // await UpdateThumbStatus( image, conversionResult );
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
                    Logging.LogError($"Unable to process /thumb/{thumbSize}/{imageId}: {ex.Message}");
                }
            }

            watch.Stop();

            return result;
        }

        private async Task UpdateThumbStatus(Image image, ImageProcessResult conversionResult)
        {
            Logging.LogTrace($" - Updating metadata for {image.ImageId}");
            try
            {
                using var db = new ImageContext();

                if (image.MetaData != null)
                {
                    db.Attach(image.MetaData);
                    image.MetaData.ThumbLastUpdated = DateTime.UtcNow;
                    db.ImageMetaData.Update(image.MetaData);
                }
                else
                {
                    var metadata = new ImageMetaData
                    {
                        ImageId = image.ImageId,
                        ThumbLastUpdated = DateTime.UtcNow
                    };
                    db.ImageMetaData.Add(metadata);
                    image.MetaData = metadata;
                }

                await db.SaveChangesAsync("ThumbUpdate");
            }
            catch (Exception ex)
            {
                Logging.LogWarning($"Unable to update DB thumb for ID {image.ImageId}: {ex.Message}");
            }
        }

        [HttpGet("/face/{faceId}")]
        public async Task<IActionResult> Face(string faceId, CancellationToken cancel,
                [FromServices] ImageProcessService imageProcessor,
                [FromServices] ThumbnailService thumbService,
                [FromServices] ImageCache imageCache)
        {
            using var db = new ImageContext();

            IActionResult result = Redirect("/no-image.png");

            try
            {
                var query = db.ImageObjects.AsQueryable();

                // TODO Massively optimise this - if the file already exists we don't need the DB
                if (int.TryParse(faceId, out var personId))
                {
                    query = query.Where(x => x.Person.PersonId == personId);
                }
                else
                {
                    query = query.Where(x => x.Person.AzurePersonId == faceId);
                }

                // Sort by largest face picture, then by most recent date taken
                var face = await query
                                .OrderByDescending(x => x.RectWidth)
                                .ThenByDescending(x => x.RectHeight)
                                .ThenByDescending(x => x.Image.SortDate)
                                .FirstOrDefaultAsync();

                if (cancel.IsCancellationRequested)
                    return result;

                if (face != null)
                {
                    var thumbPath = await thumbService.GenerateFaceThumb(face);

                    if (thumbPath != null && thumbPath.Exists)
                    {
                        result = PhysicalFile(thumbPath.FullName, "image/jpeg");
                    }
                }
            }
            catch( Exception ex )
            {
                Logging.LogError($"Unable to load face thumbnail for {faceId}: {ex.Message}");
            }

            return result;
        }
       
    }
}