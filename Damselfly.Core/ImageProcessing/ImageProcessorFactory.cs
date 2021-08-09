using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.ImageProcessing
{
    public class ImageProcessorFactory
    {
        private ImageMagickProcessor imProcessor;
        private SkiaSharpProcessor skiaProcessor;
        private ImageSharpProcessor isharpProcessor;
        private string rootContentPath;

        public void SetContentPath(string path)
        {
            rootContentPath = path;
        }

        /// <summary>
        /// Takes a file extension, and returns an ImageProcessor that can generate
        /// thumbnails for that file type. 
        /// </summary>
        /// <param name="fileExtension"></param>
        /// <returns></returns>
        public IImageProcessor GetProcessor( string fileExtension )
        {
            if( ! fileExtension.StartsWith( "." ) )
            {
                fileExtension = $".{fileExtension}";
            }

            if( SkiaSharpProcessor.SupportedFileExtensions.Any( x => x.Equals( fileExtension, StringComparison.OrdinalIgnoreCase ) ) )
            {
                if (skiaProcessor == null)
                    skiaProcessor = new SkiaSharpProcessor();

                return skiaProcessor;
            }

            if (ImageSharpProcessor.SupportedFileExtensions.Any(x => x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
            {
                if (isharpProcessor == null)
                {
                    isharpProcessor = new ImageSharpProcessor();
                    isharpProcessor.SetFontPath(Path.Combine(rootContentPath, "fonts"));
                }
                return isharpProcessor;
            }

            if (ImageMagickProcessor.SupportedFileExtensions.Any(x => x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
            {
                if (imProcessor == null)
                    imProcessor = new ImageMagickProcessor();

                return imProcessor;
            }

            return null;
        }
    }
}
