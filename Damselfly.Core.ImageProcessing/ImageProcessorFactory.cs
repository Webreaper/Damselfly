using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Damselfly.Core.Interfaces;
using Damselfly.ML.Face.Emgu;

namespace Damselfly.Core.ImageProcessing
{
    public class ImageProcessorFactory : IImageProcessorFactory
    {
        private ImageMagickProcessor imProcessor;
        private SkiaSharpProcessor skiaProcessor;
        private ImageSharpProcessor isharpProcessor;
        private EmguFaceService _emguHashProvider;

        public ImageProcessorFactory( EmguFaceService emguFaceService )
        {
            _emguHashProvider = emguFaceService;
            skiaProcessor = new SkiaSharpProcessor();
            isharpProcessor = new ImageSharpProcessor();
            imProcessor = new ImageMagickProcessor();
        }

        public void SetContentPath(string path)
        {
            isharpProcessor.SetFontPath(Path.Combine(path, "fonts"));
        }

        /// <summary>
        /// Get a perceptual hash provider.
        /// </summary>
        /// <returns></returns>
        public IHashProvider GetHashProvider()
        {
            return isharpProcessor;

            // EMGU may be faster or better, but needs testing etc.
            // if (_emguHashProvider.ServiceAvailable)
            //    return _emguHashProvider;
        }

        /// <summary>
        /// Takes a file extension, and returns an ImageProcessor that can generate
        /// thumbnails for that file type. 
        /// </summary>
        /// <param name="fileExtension"></param>
        /// <returns></returns>
        public IImageProcessor? GetProcessor( string fileExtension )
        {
            if( ! fileExtension.StartsWith( "." ) )
            {
                fileExtension = $".{fileExtension}";
            }

            // Skiasharp first. As of 12-Aug-2021, it can do thumbs for 100 images in about 23 seconds
            if (SkiaSharpProcessor.SupportedFileExtensions.Any(x => x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
            {
                return skiaProcessor;
            }

            // ImageSharp next. As of 12-Aug-2021, it can do thumbs for 100 images in about 60 seconds
            if (ImageSharpProcessor.SupportedFileExtensions.Any(x => x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
            {
                return isharpProcessor;
            }

            // ImageMagick last, because of the complexities of spawning a child process.
            // As of 12-Aug-2021, it can do thumbs for 100 images in about 33 seconds.
            // Main advantage: it can also handle HEIC
            if (ImageMagickProcessor.SupportedFileExtensions.Any(x => x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
            {
                return imProcessor;
            }

            return null;
        }
    }
}
