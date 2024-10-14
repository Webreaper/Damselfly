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

    private Yolo? _yoloModel;
    
    private Yolo GetYoloModel()
    {
        if( _yoloModel == null )
        {
            string modelPath = "./Models/yolo11n.onnx";

            if( ! File.Exists(modelPath) )
            {
                modelPath = "../Damselfly.ML.ObjectDetection.ML/Models/yolo11n.onnx";

                if( ! File.Exists(modelPath) )
                    throw new FileNotFoundException("Model not found for object detection!");
            }

            Logging.Log($"Found YoloDotNet model in {modelPath}");

            _yoloModel = new Yolo(new YoloOptions()
            {
                OnnxModel = modelPath,
                ModelVersion = ModelVersion.V11,
                Cuda = false,
                PrimeGpu = false,
                ModelType = ModelType.ObjectDetection,
            });
        }

        return _yoloModel;
    }
    
    /// <summary>
    ///     Given an image, detect objects in it using the Yolo v5 model.
    /// </summary>
    /// <param name="imageFile"></param>
    /// <returns></returns>
    public Task<IList<ImageDetectResult>> DetectObjects(Image<Rgb24> imageSharpImage, string fullpath)
    {
        IList<ImageDetectResult>? result = null;

        // Do something better here when we get an answer to:
        // https://stackoverflow.com/questions/79085360/how-to-convert-an-imagesharp-image-to-a-skiasharp-skimage
        
        var image = SKImage.FromEncodedData(fullpath);
        
        try
        {
            var watch = new Stopwatch( "DetectObjects" );

            // There's a min of 640x640 for the model.
            if( image.Width > 640 && image.Height > 640 )
            {
                var yolo = GetYoloModel();
                
                switch ( yolo.OnnxModel.ModelType )
                {
                    case ModelType.Classification:
                    {
                        var detections = yolo.RunClassification(image, 1);
                        var imageClassification = detections.OrderDescending()
                            .Select( x => x.Label)
                            .FirstOrDefault();
                            
                        break;
                    }
                    case ModelType.ObjectDetection:
                    {
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
                                    ServiceModel = "ONNX/Yolo"
                                    }
                                )
                            .ToList();
                        break;
                    }
                }

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
}