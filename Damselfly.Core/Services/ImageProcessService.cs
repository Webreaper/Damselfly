using System.IO;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Interfaces;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using System;

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
        private readonly IImageProcessor _processor;

        public ImageProcessService(IImageProcessor processor)
        {
            _processor = processor;

            Logging.Log($"Initialised {processor.GetType().Name} for thumbnail processing.");
        }

        public void SetContentPath( string path )
        {
            if( _processor is ImageSharpProcessor imageSharp )
                imageSharp.SetFontPath(Path.Combine(path, "fonts"));
        }

        public async Task<ImageProcessResult> CreateThumbs(FileInfo source, IDictionary<FileInfo, ThumbConfig> destFiles)
        {
            return await _processor.CreateThumbs(source, destFiles);
        }

        public void TransformDownloadImage(string input, Stream output, ExportConfig config)
        {
            _processor.TransformDownloadImage(input, output, config);
        }

        public bool IsImageFileType(FileInfo filename)
        {
            if (filename.IsHidden())
                return false;

            return _processor.SupportedFileExtensions.Any(x => x.Equals(filename.Extension, StringComparison.OrdinalIgnoreCase));
        }


        public ICollection<string> SupportedFileExtensions { get{ return _processor.SupportedFileExtensions; } }
    }
}
