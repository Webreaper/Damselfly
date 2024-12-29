using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.ML;
using Damselfly.ML.ImageClassification.DominantColors;
using Damselfly.Shared.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.Models;
using Rectangle = System.Drawing.Rectangle;

namespace Damselfly.ML.ObjectDetection;

public class ObjectDetector
{
    private const float predictionThreshold = 0.5f;

    public System.Drawing.Color DetectDominantColour(Image<Rgb24> image)
    {
        var calculator = new DominantHueColorCalculator();

        var color = calculator.CalculateDominantColor(image);

        return color;
    }

    public System.Drawing.Color DetectAverageColor(Image<Rgb24> srcImage)
    {
        var totalRed = 0;
        var totalGreen = 0;
        var totalBlue = 0;

        var image = srcImage.Clone(x => x.Resize(new ResizeOptions
            { Sampler = KnownResamplers.NearestNeighbor, Size = new Size(100, 0) }));

        image.ProcessPixelRows(pixelAccessor =>
        {
            for ( var y = 0; y < pixelAccessor.Height; y++ )
            {
                var row = pixelAccessor.GetRowSpan(y);

                for ( var x = 0; x < row.Length; x++ )
                {
                    totalRed += row[x].R;
                    totalGreen += row[x].G;
                    totalBlue += row[x].B;
                }
            }
        });

        var totalPixels = image.Width * image.Height;
        var avgRed = (byte)(totalRed / totalPixels);
        var avgGreen = (byte)(totalGreen / totalPixels);
        var avgBlue = (byte)(totalBlue / totalPixels);
        return System.Drawing.Color.FromArgb(avgRed, avgGreen, avgBlue);
    }

    private Dictionary<ModelType, Yolo> _yoloModels = new();

    private Yolo GetYoloModel(ModelType modelType)
    {
        if( ! _yoloModels.TryGetValue(modelType, out var yolo) )
        {
            var modelFile = modelType switch
            {
                ModelType.ObjectDetection => "yolo11n.onnx",
                ModelType.Classification => "yolo11n-cls.onnx",
                _ => throw new NotImplementedException($"Unknown model type: {modelType}")
            };

            var modelPath = $"./Models/{modelFile}";

            if( ! File.Exists(modelPath) )
            {
                modelPath = $"../Damselfly.ML.ObjectDetection.ML/Models/{modelFile}";

                if( ! File.Exists(modelPath) )
                    throw new FileNotFoundException("Model not found for object detection!");
            }

            Logging.Log($"Found YoloDotNet model in {modelPath}");

            yolo = new Yolo(new YoloOptions()
            {
                OnnxModel = modelPath,
                Cuda = false,
                PrimeGpu = false,
                ModelType = modelType
            });

            _yoloModels.Add(modelType, yolo);
        }

        return yolo;
    }

    public static SKImage CreateSkImageFromImageSharp(Image<Rgb24> imageSharpImage)
    {
        var buffer = new byte[imageSharpImage.Width * imageSharpImage.Height * 4];

        var pixel = 0;
        // First, convert from an image, to an array of RGB float values. 
        imageSharpImage.ProcessPixelRows(pixelAccessor =>
        {
            for ( var y = 0; y < pixelAccessor.Height; y++ )
            {
                var row = pixelAccessor.GetRowSpan(y);
                for( var x = 0; x < pixelAccessor.Width; x++ )
                {
                    buffer[pixel * 4 + 0] = row[x].R;
                    buffer[pixel * 4 + 1] = row[x].G;
                    buffer[pixel * 4 + 2] = row[x].B;
                    buffer[pixel * 4 + 3] = 255; // Alpha
                    pixel++;
                }
            }
        });
        var image = SKImage.FromPixelCopy(
            new SKImageInfo(imageSharpImage.Width, imageSharpImage.Height, SKColorType.Rgb888x), buffer);

        return image;
    }

    /// <summary>
    ///     Given an image, detect objects in it using the Yolo v5 model.
    /// </summary>
    /// <param name="imageSharpImage"></param>
    /// <returns></returns>
    public Task<IList<ImageDetectResult>> DetectObjects(Image<Rgb24> imageSharpImage)
    {
        IList<ImageDetectResult>? result = null;

        var image = CreateSkImageFromImageSharp(imageSharpImage);

        try
        {
            var watch = new Stopwatch( "DetectObjects" );

            // There's a min of 640x640 for the model.
            if( image is { Width: > 640, Height: > 640 } )
            {
                var yolo = GetYoloModel(ModelType.ObjectDetection);

                var detections = yolo.RunObjectDetection(image);
                result = detections.Where( x => x.Confidence > predictionThreshold )
                    .Select(x =>
                        new ImageDetectResult
                        {
                            Rect = new Rectangle
                            {
                                X = x.BoundingBox.Left,
                                Y = x.BoundingBox.Top,
                                Width = x.BoundingBox.Width,
                                Height = x.BoundingBox.Height
                            },
                            Tag = x.Label.Name,
                            Service = "YoloDotNet",
                            ServiceModel = "Yolo11"
                        }
                    )
                    .ToList();

                watch.Stop();
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Error during object detection: {ex.Message}");
        }

        if( result == null )
            result = new List<ImageDetectResult>();

        return Task.FromResult( result );
    }


    /// <summary>
    ///     Given an image, detect objects in it using the Yolo v5 model.
    /// </summary>
    /// <param name="imageSharpImage"></param>
    /// <returns></returns>
    public Task<string?> ClassifyImage(Image<Rgb24> imageSharpImage)
    {
        string? classification = null;

        var image = CreateSkImageFromImageSharp(imageSharpImage);

        try
        {
            var watch = new Stopwatch( "ClassifyImage" );

            // There's a min of 640x640 for the model.
            if( image is { Width: > 640, Height: > 640 } )
            {
                var yolo = GetYoloModel(ModelType.Classification);

                var detections = yolo.RunClassification(image, 1);
                classification = detections.OrderDescending()
                    .Select( x => x.Label)
                    .FirstOrDefault();

                watch.Stop();
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Error during image classification: {ex.Message}");
        }

        return Task.FromResult( classification );
    }
}