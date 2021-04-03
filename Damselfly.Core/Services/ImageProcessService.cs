using System.IO;
using System.Collections.Generic;
using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Interfaces;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
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
    public class ImageProcessService : IImageProcessor
    {
        public static ImageProcessService Instance { get; private set; }
        private readonly IImageProcessor processor;

        public static bool UseImageSharp { get; set; }

        public ImageProcessService()
        {
            Instance = this;

            if (UseImageSharp)
                processor = new ImageSharpProcessor();
            else
                processor = new SkiaSharpProcessor();

            Logging.Log($"Initialised {processor.GetType().Name} for thumbnail processing.");
        }

        public void SetContentPath( string path )
        {
            if( processor is ImageSharpProcessor imageSharp )
                imageSharp.SetFontPath(Path.Combine(path, "fonts"));
        }

        public async Task<ImageProcessResult> CreateThumbs(FileInfo source, IDictionary<FileInfo, ThumbConfig> destFiles)
        {
            return await processor.CreateThumbs(source, destFiles);
        }

        public void TransformDownloadImage(string input, Stream output, string waterMarkText = null)
        {
            processor.TransformDownloadImage(input, output, waterMarkText);
        }

        public ICollection<string> SupportedFileExtensions { get{ return processor.SupportedFileExtensions; } }
    }
}
