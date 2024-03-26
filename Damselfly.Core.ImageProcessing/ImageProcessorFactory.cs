using Damselfly.Core.Interfaces;

namespace Damselfly.Core.ImageProcessing;

public class ImageProcessorFactory : IImageProcessorFactory
{
    private readonly ImageMagickProcessor imProcessor;
    private readonly ImageSharpProcessor isharpProcessor;
    private readonly SkiaSharpProcessor skiaProcessor;
    private readonly MagickNetProcessor magickNetProcessor;

    public ImageProcessorFactory()
    {
        skiaProcessor = new SkiaSharpProcessor();
        isharpProcessor = new ImageSharpProcessor();
        imProcessor = new ImageMagickProcessor();
        magickNetProcessor = new MagickNetProcessor();
    }

    public void SetContentPath(string path)
    {
        isharpProcessor.SetFontPath(Path.Combine(path, "fonts"));
    }

    /// <summary>
    ///     Get a perceptual hash provider.
    /// </summary>
    /// <returns></returns>
    public IHashProvider GetHashProvider()
    {
        return isharpProcessor;
    }

    /// <summary>
    ///     Takes a file extension, and returns an ImageProcessor that can generate
    ///     thumbnails for that file type.
    /// </summary>
    /// <param name="fileExtension"></param>
    /// <returns></returns>
    public IImageProcessor? GetProcessor(string fileExtension)
    {
        if ( !fileExtension.StartsWith(".") ) fileExtension = $".{fileExtension}";

        // Skiasharp first. As of 12-Aug-2021, it can do thumbs for 100 images in about 23 seconds
        if ( SkiaSharpProcessor.SupportedFileExtensions.Any(x =>
                x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)) ) return skiaProcessor;

        // ImageSharp next. As of 12-Aug-2021, it can do thumbs for 100 images in about 60 seconds
        if ( ImageSharpProcessor.SupportedFileExtensions.Any(x =>
                x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)) ) return isharpProcessor;

        // ImageMagick last, because of the complexities of spawning a child process.
        // As of 12-Aug-2021, it can do thumbs for 100 images in about 33 seconds.
        // Main advantage: it can also handle HEIC
        // Mar 2024 - disable this in preference to Magick.Net
        //if ( ImageMagickProcessor.SupportedFileExtensions.Any(x => 
        //        x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)) ) return imProcessor;

        // Magick.Net - As of 12-Aug-2024, it can do thumbs for 100 images in about 45 seconds.
        // Main advantage: it can also handle HEIC, and is native
        if ( MagickNetProcessor.SupportedFileExtensions.Any(x =>
                x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)) ) return magickNetProcessor;

        return null;
    }
}