using System.IO;
using System.Collections.Generic;
using Damselfly.Core.Interfaces;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.Images;

namespace Damselfly.Core.Services;

/// <summary>
/// Abstraction over the different image processing libraries.
/// In testing, SkiaSharp was shown to be significantly faster
/// then ImageSharp:
///   SkiaSharp: average 100 image thumbnails processed in 50s
///   ImageSharp: average 100 image thumnbails processed in 110s
///   GraphicsMagick: average 100 image thumnbails processed in 100s
/// However, since ImageSharp is entirely .Net native, whereas
/// SkiaSharp requires C++ binaries, we'll provide both for
/// compatibility (slower performance is better than none...).
/// </summary>
public class ImageProcessService : IImageProcessor, IHashProvider
{
    private readonly IImageProcessorFactory _factory;

    public ImageProcessService( IImageProcessorFactory factory )
    {
        _factory = factory;
    }

    public void SetContentPath( string path )
    {
        _factory.SetContentPath(path);
    }

    public string GetPerceptualHash( string path )
    {
        var provider = _factory.GetHashProvider();

        var watch = new Stopwatch("GenPerceptualHash");
        var hash = provider.GetPerceptualHash(path);
        watch.Stop();

        return hash;
    }

    /// <summary>
    /// Creates a set of thumbs for an input image
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destFiles"></param>
    /// <returns></returns>
    public async Task<ImageProcessResult> CreateThumbs(FileInfo source, IDictionary<FileInfo, ThumbConfig> destFiles)
    {
        var processor = _factory.GetProcessor(source.Extension);

        if (processor != null)
        {
            var result = await processor.CreateThumbs(source, destFiles);

            return result;
        }

        return new ImageProcessResult { ThumbsGenerated = false };
    }

    /// <summary>
    /// Convert an image, optionally watermarking.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <param name="config"></param>
    /// TODO: Async
    public async Task TransformDownloadImage(string input, Stream output, IExportSettings exportConfig)
    {
        var ext = Path.GetExtension(input);

        var processor = _factory.GetProcessor(ext);

        if (processor != null)
            await processor.TransformDownloadImage(input, output, exportConfig);
    }

    /// <summary>
    /// Returns true if the file is one that we consider to be an image - that is,
    /// one that we have an image processor for, which will generate thumbs, etc.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public bool IsImageFileType(FileInfo filename)
    {
        if (filename.IsHidden())
            return false;

        var processor = _factory.GetProcessor(filename.Extension);

        // If we have a valid processor, we're good. 
        return processor != null;
    }

    public async Task GetCroppedFile(FileInfo source, int x, int y, int width, int height, FileInfo destFile)
    {
        var ext = Path.GetExtension(source.Name);

        var processor = _factory.GetProcessor(ext);

        if (processor != null)
            await processor.GetCroppedFile(source, x, y, width, height, destFile);
    }

    public async Task CropImage(FileInfo path, int x, int y, int width, int height, Stream stream)
    {
        var processor = _factory.GetProcessor(path.Extension);

        if (processor != null)
            await processor.CropImage(path, x, y, width, height, stream);
    }
}
