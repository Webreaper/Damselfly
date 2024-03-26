using Damselfly.Core.DbModels.Images;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Shared.Utils;
using ImageMagick;

namespace Damselfly.Core.ImageProcessing;

public class MagickNetProcessor : IImageProcessor
{
    public static ICollection<string> SupportedFileExtensions => SupportedExtensions();

    private static ICollection<string> SupportedExtensions()
    {
        // Interrogate Magick.Net for a list of supported file extensions where we can read and write the image
        var formats = MagickNET.SupportedFormats.Where( format => format.SupportsReading && format.SupportsWriting )
            .Select( format => $".{format.Format.ToString().ToLower()}").ToList();

        return formats;
    }
    
    /// <summary>
    /// Convert the files to thumbnails using Magick.Net
    /// </summary>
    /// <param name="source">Source.</param>
    /// <param name="sizes">Sizes.</param>
    public async Task<IImageProcessResult> CreateThumbs(FileInfo source, IDictionary<FileInfo, IThumbConfig> destFiles)
    {
        // This processor doesn't support hash creation
        IImageProcessResult result = new ImageProcessResult { ThumbsGenerated = false, ImageHash = string.Empty };

        using var image = new MagickImage();

        var load = new Stopwatch("MagickNetLoad");

        await image.ReadAsync(source.FullName);
        image.AutoOrient();
        
        load.Stop();
        
        var thumbs = new Stopwatch("MagickNetThumbs");

        foreach ( var pair in destFiles )
        {
            var dest = pair.Key;
            var config = pair.Value;

            var targetRatio = (decimal)config.width / (decimal)config.height; 
            var currentRatio = (decimal)image.Width/(decimal)image.Height;

            bool widthIsLongest = currentRatio > targetRatio;

            var intermediateSize = widthIsLongest
                ? new MagickGeometry { Width = (int)(config.height * currentRatio), Height = config.height }
                : new MagickGeometry { Width = config.width, Height = (int)(config.width / currentRatio) };
            
            Logging.LogTrace("Generating thumbnail for {0}: {1}x{2}", source.Name, config.width, config.height);

            image.Resize(intermediateSize);
            intermediateSize.IgnoreAspectRatio = false;

            if( currentRatio != targetRatio && config.cropToRatio)
            {
                var size = new MagickGeometry(config.width, config.height);

                image.Crop(size, Gravity.Center);
            }

            await image.WriteAsync(dest.FullName);

            result.ThumbsGenerated = true;
        }

        thumbs.Stop();
        
        return result;    
    }

    public Task TransformDownloadImage(string input, Stream output, IExportSettings config)
    {
        throw new NotImplementedException();
    }

    public Task GetCroppedFile(FileInfo source, int x, int y, int width, int height, FileInfo destFile)
    {
        throw new NotImplementedException();
    }

    public Task CropImage(FileInfo source, int x, int y, int width, int height, Stream stream)
    {
        throw new NotImplementedException();
    }
}