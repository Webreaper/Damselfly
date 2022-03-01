using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.Interfaces;
using SkiaSharp;
using Damselfly.Core.Utils;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils.Images;

namespace Damselfly.Core.ImageProcessing
{
    public class SkiaSharpProcessor : IImageProcessor
    {
        // SkiaSharp doesn't handle .heic files... yet
        private static readonly string[] s_imageExtensions = { ".jpg", ".jpeg", ".png", /*".heic", */".webp", ".bmp", ".dng", ".cr2", ".orf", ".nef"};

        public static ICollection<string> SupportedFileExtensions { get { return s_imageExtensions; } }

        /// <summary>
        /// Create an SHA1 hash from the image data (pixels only) to allow us to find
        /// duplicate images. Note that this ignores EXIF metadata, so the hash will
        /// find duplicate images even if the metadata is different.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>String hash of the image data</returns>
        public static string? GetHash(SKBitmap sourceBitmap)
        {
            string? result = null;

            try
            {
                var hashWatch = new Stopwatch("HashImage");

                var pixels = sourceBitmap.GetPixelSpan();

                var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

                for (int row = 0; row < sourceBitmap.Height; row++)
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
        public Task<ImageProcessResult> CreateThumbs(FileInfo source, IDictionary<FileInfo, ThumbConfig> destFiles)
        {
            Stopwatch load, hashThumb, scale, save, thumbs;
            ImageProcessResult result = new ImageProcessResult { ThumbsGenerated = false };

            try
            {
                thumbs = new Stopwatch("GenThumbs");

                int desiredWidth = destFiles.Max(x => x.Value.width);

                load = new Stopwatch("LoadThumb");
                using var sourceBitmap = LoadOrientedBitmap(source, desiredWidth);
                load.Stop();

                hashThumb = new Stopwatch("HashThumb");
                result.ImageHash = GetHash(sourceBitmap);
                hashThumb.Stop();

                // Dropping this from High to Low doesn't have that much of an effect
                // in terms of image quality.
                var quality = SKFilterQuality.Medium;
                var srcBitmap = sourceBitmap;

                foreach (var pair in destFiles.OrderByDescending(x => x.Value.width))
                {
                    var dest = pair.Key;
                    var config = pair.Value;

                    scale = new Stopwatch("ScaleThumb");

                    float widthScaleFactor = (float)srcBitmap.Width / (float)config.width;
                    float heighScaleFactor = (float)srcBitmap.Height / (float)config.height;
                    float scaleFactor = Math.Min(widthScaleFactor, heighScaleFactor);

                    using var scaledImage = new SKBitmap((int)(srcBitmap.Width / scaleFactor), (int)(srcBitmap.Height / scaleFactor));
                    srcBitmap.ScalePixels(scaledImage.PeekPixels(), quality);

                    var cropSize = new SKSize { Height = config.height, Width = config.width };
                    using var cropped = config.cropToRatio ? Crop(scaledImage, cropSize) : scaledImage;

                    using SKData data = cropped.Encode(SKEncodedImageFormat.Jpeg, 90);

                    scale.Stop();
                    save = new Stopwatch("SaveThumb");
                    // TODO: For configs flagged batchcreate == false, perhaps don't write to disk
                    // and just pass back the stream?
                    using (var stream = new FileStream(dest.FullName, FileMode.Create, FileAccess.Write))
                        data.SaveTo(stream);
                    save.Stop();

                    // Now, use the previous scaled image as the basis for the
                    // next smaller thumbnail. This should reduce processing
                    // time as we only work on the large image on the first
                    // iteration
                    if (destFiles.Count > 1)
                        srcBitmap = scaledImage.Copy();

                    result.ThumbsGenerated = true;
                    // TODO: Dispose

                    if (pair.Value.size == ThumbSize.ExtraLarge)
                        Logging.Log($"{pair.Value.size} thumb created for {source.Name} [load: {load.ElapsedTime}ms, scale: {scale.ElapsedTime}ms, save: {save.ElapsedTime}ms]");
                }

                thumbs.Stop();
            }
            catch (Exception ex)
            {
                Logging.Log($"Exception during Thumbnail processing: {ex.Message}");
                throw;
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Crop a file
        /// </summary>
        /// <param name="source"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        public Task CropImage(FileInfo source, int x, int y, int width, int height, Stream destStream)
        {
            Stopwatch watch = new Stopwatch("SkiaSharpCrop");

            try
            {
                using SKBitmap sourceBitmap = SKBitmap.Decode(source.FullName);

                // setup crop rect
                var cropRect = new SKRectI(x, y, x + width, y + height);
                var cropped = Crop(sourceBitmap, cropRect);
                using SKData data = cropped.Encode(SKEncodedImageFormat.Jpeg, 90);
                data.SaveTo(destStream);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Exception during Skia Crop processing: {ex.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Crops a file, saving it to disk
        /// </summary>
        /// <param name="source"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        public async Task GetCroppedFile(FileInfo source, int x, int y, int width, int height, FileInfo dest)
        {
            using (var stream = new FileStream(dest.FullName, FileMode.Create, FileAccess.Write))
            {
                await CropImage(source, x, y, width, height, stream);
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

                return Crop(original, cropRect);
            }
            else
                return original.Copy();
        }

        /// <summary>
        /// Crop to a rectangle
        /// </summary>
        /// <param name="original"></param>
        /// <param name="cropRect"></param>
        /// <returns></returns>
        private SKBitmap Crop( SKBitmap original, SKRectI cropRect)
        {
            // crop
            SKBitmap bitmap = new SKBitmap(cropRect.Width, cropRect.Height);
            original.ExtractSubset(bitmap, cropRect);
            return bitmap;
        }

        /// <summary>
        /// Loads an image from a disk file, decoding for the optimal required
        /// size so that we don't load the entire image for a smaller target,
        /// and auto-orienting the bitmap according to the codec origin.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="desiredWidth"></param>
        /// <returns></returns>
        private SKBitmap SlowLoadOrientedBitmap(FileInfo source, int desiredWidth)
        {
            Stopwatch load = new Stopwatch("SkiaSharpLoad");

            using SKImage img = SKImage.FromEncodedData(source.FullName);

            var bmp = SKBitmap.FromImage(img);

            Logging.LogTrace($"Loaded {source.Name} - loaded size = W: {bmp.Width}, H: {bmp.Height}");

            load.Stop();

            return bmp;
        }

        /// <summary>
        /// Loads an image from a disk file, decoding for the optimal required
        /// size so that we don't load the entire image for a smaller target,
        /// and auto-orienting the bitmap according to the codec origin.
        /// This is faster than the slow method above, because it uses the trick
        /// outlined here:
        /// https://forums.xamarin.com/discussion/88794/fast-way-to-decode-skbitmap-from-byte-jpeg-raw
        /// </summary>
        /// <param name="source"></param>
        /// <param name="desiredWidth"></param>
        /// <returns></returns>
        private SKBitmap LoadOrientedBitmap(FileInfo source, int desiredWidth)
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

            // First, auto-orient the bitmap
            var sourceBitmap = AutoOrient(bmp, codec.EncodedOrigin);

            return sourceBitmap;
        }

        /// <summary>
        /// Needed for auto-orienting the image after using the Scaled codec to load
        /// the image. 
        /// </summary>
        /// <param name="original"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        private static SKBitmap AutoOrient(SKBitmap original, SKEncodedOrigin origin)
        {
            Stopwatch orient = new Stopwatch("SkiaSharpOrient");
                
            var useWidth = original.Width;
            var useHeight = original.Height;
            Action<SKCanvas> transform = canvas => { };
            switch (origin)
            {
                case SKEncodedOrigin.TopLeft:
                    break;
                case SKEncodedOrigin.TopRight:
                    // flip along the x-axis
                    transform = canvas => canvas.Scale(-1, 1, useWidth / 2, useHeight / 2);
                    break;
                case SKEncodedOrigin.BottomRight:
                    transform = canvas => canvas.RotateDegrees(180, useWidth / 2, useHeight / 2);
                    break;
                case SKEncodedOrigin.BottomLeft:
                    // flip along the y-axis
                    transform = canvas => canvas.Scale(1, -1, useWidth / 2, useHeight / 2);
                    break;
                case SKEncodedOrigin.LeftTop:
                    useWidth = original.Height;
                    useHeight = original.Width;
                    transform = canvas =>
                    {
                        // Rotate 90
                        canvas.RotateDegrees(90, useWidth / 2, useHeight / 2);
                        canvas.Scale(useHeight * 1.0f / useWidth, -useWidth * 1.0f / useHeight, useWidth / 2, useHeight / 2);
                    };
                    break;
                case SKEncodedOrigin.RightTop:
                    useWidth = original.Height;
                    useHeight = original.Width;
                    transform = canvas =>
                    {
                        canvas.Translate(useWidth, 0);
                        canvas.RotateDegrees(90);
                    };
                    break;
                case SKEncodedOrigin.RightBottom:
                    useWidth = original.Height;
                    useHeight = original.Width;
                    transform = canvas =>
                    {
                        // Rotate 90
                        canvas.RotateDegrees(90, useWidth / 2, useHeight / 2);
                        canvas.Scale(-useHeight * 1.0f / useWidth, useWidth * 1.0f / useHeight, useWidth / 2, useHeight / 2);
                    };
                    break;
                case SKEncodedOrigin.LeftBottom:
                    useWidth = original.Height;
                    useHeight = original.Width;
                    transform = canvas =>
                    {
                        canvas.Translate(0, useHeight);
                        canvas.RotateDegrees(270);
                    };
                    break;
                default:
                    break;
            }

            var rotated = new SKBitmap(useWidth, useHeight);
            using (var canvas = new SKCanvas(rotated))
            {
                transform.Invoke(canvas);
                canvas.DrawBitmap(original, 0, 0);
            }

            orient.Stop();

            return rotated;
        }

        /// <summary>
        /// Async wrapper - note that Skia Sharp doesn't support Async yet.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task TransformDownloadImage(string input, Stream output, IExportSettings config)
        {
            await Task.Run(() => TransformDownloadImageSync(input, output, config));
        }

        /// <summary>
        /// Transform the images ready for download, optionally adding a watermark.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="waterMarkText"></param>
        public void TransformDownloadImageSync(string input, Stream output, IExportSettings config)
        {
            using SKImage img = SKImage.FromEncodedData(input);
            using var bitmap = SKBitmap.FromImage(img);

            float maxSize = config.MaxImageSize;
            var resizeFactor = 1f;

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

            if (!string.IsNullOrEmpty(config.WatermarkText))
            {
                using var font = SKTypeface.FromFamilyName("Arial");
                using var brush = new SKPaint
                {
                    Typeface = font,
                    TextSize = 64.0f,
                    IsAntialias = true,
                    Color = new SKColor(255, 255, 255, 255)
                };

                var textWidth = brush.MeasureText(config.WatermarkText);
                var textTargetWidth = targetWidth / 6f;
                var fontScale = textTargetWidth / textWidth;

                brush.TextSize *= fontScale;

                // Offset by text width + 10%
                var rightOffSet = (textTargetWidth * 1.1f);

                canvas.DrawText(config.WatermarkText, targetWidth - rightOffSet, targetHeight - brush.TextSize, brush);
            }

            canvas.Flush();

            using var image = SKImage.FromBitmap(toBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);

            data.SaveTo(output);
        }
    }
}
