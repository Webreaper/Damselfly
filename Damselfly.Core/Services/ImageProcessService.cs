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
        private readonly ImageProcessorFactory _factory;

        public ImageProcessService()
        {
            _factory = new ImageProcessorFactory();
        }

        public void SetContentPath( string path )
        {
            _factory.SetContentPath(path);
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

            if( processor != null )
                return await processor.CreateThumbs(source, destFiles);

            return new ImageProcessResult { ThumbsGenerated = false };
        }

        /// <summary>
        /// Convert an image, optionally watermarking.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="config"></param>
        public void TransformDownloadImage(string input, Stream output, ExportConfig config)
        {
            var ext = Path.GetExtension(input);

            var processor = _factory.GetProcessor(ext);

            if (processor != null)
                processor.TransformDownloadImage(input, output, config);
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
    }
}
