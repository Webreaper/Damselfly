using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.Interfaces;
using SkiaSharp;
using Damselfly.Core.Utils;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Damselfly.Core.ImageProcessing
{
    public class SkiaSharpProcessor : IImageProcessor
    {
        // SkiaSharp doesn't handle .heic files... yet
        private static readonly string[] s_imageExtensions = { ".jpg", ".jpeg", ".png", /*".heic", */".webp", ".bmp", ".dng" };

        public ICollection<string> SupportedFileExtensions { get { return s_imageExtensions; } }

        /// <summary>
        /// Create an SH1 hash from the image data (pixels only) to allow us to find
        /// duplicate images. Note that this ignores EXIF metadata, so the hash will
        /// find duplicate images even if the metadata is different.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>String hash of the image data</returns>
        public static string GetHash( SKBitmap sourceBitmap )
        {
            string result = null;

            try
            {
                var hashWatch = new Stopwatch("HashImage");

                var pixels = sourceBitmap.GetPixelSpan();

                var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

                for ( int row = 0; row < sourceBitmap.Height; row++ )
                {
                    var rowPixels = pixels.Slice(row * sourceBitmap.RowBytes, sourceBitmap.RowBytes);

                    byte[] rgbaBytes = MemoryMarshal.AsBytes(rowPixels).ToArray();
                    hash.AppendData(rgbaBytes);
                }

                result = hash.GetHashAndReset().ToHex(true);
                hashWatch.Stop();
                Logging.LogVerbose($"Hashed image ({result}) in {hashWatch.HumanElapsedTime}");
            }
            catch (Exception ex)
            {
                Logging.LogError($"Exception while calculating hash: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Resize using SkiaSharp - this can do 100 images in about 30s (2020 i5 MacBook Air).
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destFiles"></param>
        public void CreateThumbs(FileInfo source, IDictionary<FileInfo, ThumbConfig> destFiles, out string imageHash )
        {
            try
            {
                int desiredWidth = destFiles.Max(x => x.Value.width);

                using var sourceBitmap = LoadOrientedBitmap(source, desiredWidth);

                imageHash = GetHash( sourceBitmap );

                Stopwatch thumbs = new Stopwatch("SkiaSharpThumbs");

                // Dropping this from High to Low doesn't have that much of an effect
                // in terms of performance.
                var quality = SKFilterQuality.Low;
                var srcBitmap = sourceBitmap;

                foreach (var pair in destFiles.OrderByDescending(x => x.Value.width))
                {
                    var dest = pair.Key;
                    var config = pair.Value;

                    float widthScaleFactor = (float)srcBitmap.Width / (float)config.width;
                    float heighScaleFactor = (float)srcBitmap.Height / (float)config.height;
                    float scaleFactor = Math.Min(widthScaleFactor, heighScaleFactor);

                    using var scaledImage = new SKBitmap((int)(srcBitmap.Width / scaleFactor), (int)(srcBitmap.Height / scaleFactor));
                    srcBitmap.ScalePixels(scaledImage.PeekPixels(), quality);

                    var cropSize = new SKSize { Height = config.height, Width = config.width };
                    using var cropped = config.cropToRatio ? Crop(scaledImage, cropSize) : scaledImage;

                    using SKData data = cropped.Encode(SKEncodedImageFormat.Jpeg, 90);

                    using (var stream = new FileStream(dest.FullName, FileMode.Create, FileAccess.Write))
                        data.SaveTo(stream);

                    // Now, use the previous scaled image as the basis for the
                    // next smaller thumbnail. This should reduce processing
                    // time as we only work on the large image on the first
                    // iteration
                    srcBitmap = scaledImage.Copy();

                    // TODO: Dispose
                }

                thumbs.Stop();
            }
            catch ( Exception ex )
            {
                Logging.Log($"Exception during Skia processing: {ex.Message}");
                throw;
            }

        }

        /// <summary>
        /// Crop the image to fit within the dimensions specified. 
        /// </summary>
        /// <param name="original"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        private SKBitmap Crop(SKBitmap original, SKSize maxSize)
        {
            var cropSides = 0;
            var cropTopBottom = 0;

            // calculate amount of pixels to remove from sides and top/bottom
            if ((float)maxSize.Width / original.Width < maxSize.Height / original.Height) // crop sides
                cropSides = original.Width - (int)Math.Round((float)original.Height / maxSize.Height * maxSize.Width);
            else
                cropTopBottom = original.Height - (int)Math.Round((float)original.Width / maxSize.Width * maxSize.Height);

            if (cropSides > 0 || cropTopBottom > 0)
            {
                // setup crop rect
                var cropRect = new SKRectI
                {
                    Left = cropSides / 2,
                    Top = cropTopBottom / 2,
                    Right = original.Width - cropSides + cropSides / 2,
                    Bottom = original.Height - cropTopBottom + cropTopBottom / 2
                };

                // crop
                SKBitmap bitmap = new SKBitmap(cropRect.Width, cropRect.Height);
                original.ExtractSubset(bitmap, cropRect);
                return bitmap;
            }
            else
                return original.Copy();
        }

        /// <summary>
        /// Loads an image from a disk file, decoding for the optimal required
        /// size so that we don't load the entire image for a smaller target,
        /// and auto-orienting the bitmap according to the codec origin.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="desiredWidth"></param>
        /// <returns></returns>
        private SKBitmap LoadOrientedBitmap( FileInfo source, int desiredWidth )
        {
            Stopwatch load = new Stopwatch("SkiaSharpLoad");

            SKCodec codec = SKCodec.Create(source.FullName);
            SKImageInfo info = codec.Info;

            // get the scale that is nearest to what we want (eg: jpg returned 512)
            SKSizeI supportedScale = codec.GetScaledDimensions((float)desiredWidth / info.Width);

            // decode the bitmap at the nearest size
            SKImageInfo nearest = new SKImageInfo(supportedScale.Width, supportedScale.Height);
            SKBitmap bmp = SKBitmap.Decode(codec, nearest);

            load.Stop();

            Logging.LogTrace($"Loaded {source.Name} - loaded size = W: {bmp.Width}, H: {bmp.Height}, Orient: {codec.EncodedOrigin}");

            // First, auto-orient the bitmap
            var sourceBitmap = AutoOrient(bmp, codec.EncodedOrigin);

            return sourceBitmap;
        }


        private static SKBitmap AutoOrient(SKBitmap bitmap, SKEncodedOrigin origin)
        {
            Stopwatch orient = new Stopwatch("SkiaSharpOrient");

            SKBitmap rotated;
            switch (origin)
            {
                case SKEncodedOrigin.BottomRight:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.RotateDegrees(180, bitmap.Width / 2, bitmap.Height / 2);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    break;
                case SKEncodedOrigin.RightTop:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Translate(rotated.Width, 0);
                        canvas.RotateDegrees(90);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    break;
                case SKEncodedOrigin.LeftBottom:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Translate(0, rotated.Height);
                        canvas.RotateDegrees(270);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    break;
                default:
                    rotated = bitmap.Copy();
                    break;
            }

            orient.Stop();

            return rotated;
        }

        /// <summary>
        /// Transform the images ready for download, optionally adding a watermark.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="waterMarkText"></param>
        public void TransformDownloadImage(string input, Stream output, string waterMarkText = null)
        {
            float maxSize = 1600f;
            var resizeFactor = 1f;
            using var codec = SKCodec.Create(input);
            using var original = SKBitmap.Decode(codec);

            // TODO: Use LoadOrientedBitmap here.
            // First, auto-orient the bitmap
            using var bitmap = AutoOrient(original, codec.EncodedOrigin);

            if (bitmap.Width > maxSize)
            {
                resizeFactor = maxSize / bitmap.Width;
            }
            else if (bitmap.Height > maxSize)
            {
                resizeFactor = maxSize / bitmap.Height;
            }

            var targetWidth = (int)Math.Round(bitmap.Width * resizeFactor);
            var targetHeight = (int)Math.Round(bitmap.Height * resizeFactor);

            // First create a bitmap the right size. 
            using var toBitmap = new SKBitmap(targetWidth, targetHeight, bitmap.ColorType, bitmap.AlphaType);
            using var canvas = new SKCanvas(toBitmap);

            // Draw a bitmap rescaled
            canvas.SetMatrix(SKMatrix.CreateScale(resizeFactor, resizeFactor));
            canvas.DrawBitmap(bitmap, 0, 0);
            canvas.ResetMatrix();

            using var font = SKTypeface.FromFamilyName("Arial");
            using var brush = new SKPaint
            {
                Typeface = font,
                TextSize = 64.0f,
                IsAntialias = true,
                Color = new SKColor(255, 255, 255, 255)
            };

            var textWidth = brush.MeasureText(waterMarkText);
            var textTargetWidth = targetWidth / 6f;
            var fontScale = textTargetWidth / textWidth;

            brush.TextSize *= fontScale;

            // Offset by text width + 10%
            var rightOffSet = (textTargetWidth * 1.1f);

            canvas.DrawText(waterMarkText, targetWidth - rightOffSet, targetHeight - brush.TextSize, brush);
            canvas.Flush();

            using var image = SKImage.FromBitmap(toBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);

            data.SaveTo(output);
        }
    }
}
