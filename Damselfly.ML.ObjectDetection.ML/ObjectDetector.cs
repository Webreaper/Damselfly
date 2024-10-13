using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.ML;
using Damselfly.Shared.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.Models;
using Yolov5Net.Scorer;
using Yolov5Net.Scorer.Models;
using Rectangle = System.Drawing.Rectangle;

namespace Damselfly.ML.ObjectDetection;

public class ObjectDetector
{
    private const float predictionThreshold = 0.5f;
    private YoloScorer<YoloCocoModel>? scorer;

    public void InitScorer()
    {
        Logging.Log("Initialising ObjectDetector service.");
        try
        {
            scorer = new YoloScorer<YoloCocoModel>();
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Unexpected exception initialising Object detection: {ex}");
            scorer = null; // disable.
        }
    }

    /// <summary>
    ///     Given an image, detect objects in it using the Yolo v5 model.
    /// </summary>
    /// <param name="imageFile"></param>
    /// <returns></returns>
    public Task<IList<ImageDetectResult>> DetectObjects(Image<Rgb24> imageSharpImage, string fullpath)
    {
        IList<ImageDetectResult>? result = null;
        
        //byte[] pixelBytes = new byte[imageSharpImage.Width * imageSharpImage.Height * Unsafe.SizeOf<Rgba32>()];
        //imageSharpImage.CopyPixelDataTo((pixelBytes));
        //var image = SKImage.FromEncodedData(pixelBytes);
        
        var image = SKImage.FromEncodedData(fullpath);
        
        try
        {
            if( scorer != null )
            {
                var watch = new Stopwatch( "DetectObjects" );

                // There's a min of 640x640 for the model.
                if( image.Width > 640 && image.Height > 640 )
                {
                    using var yolo = new Yolo(new YoloOptions()
                    {
                        OnnxModel = "/Users/markotway/LocalCloud/Development/Damselfly/Damselfly.ML.ObjectDetection.ML/Models/yolo11n.onnx",
                        ModelVersion = ModelVersion.V11,
                        Cuda = false,
                        PrimeGpu = false,
                        ModelType = ModelType.ObjectDetection,
                    });

                    switch ( yolo.OnnxModel.ModelType )
                    {
                        case ModelType.Classification:
                        {
                            var detections = yolo.RunClassification(image, 1);
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
                                            X = (int)x.BoundingBox.Left,
                                            Y = (int)x.BoundingBox.Top,
                                            Width = (int)x.BoundingBox.Width,
                                            Height = (int)x.BoundingBox.Height
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
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Error during object detection: {ex.Message}");
        }

        if( result == null )
            result = new List<ImageDetectResult>();

        return Task.FromResult( result );
    }
    
    private void DrawRectangles(Image img, List<YoloPrediction> predictions)
    {
        /*
        using var graphics = Graphics.FromImage(img);

        foreach (var prediction in predictions) // iterate each prediction to draw results
        {
            double score = Math.Round(prediction.Score, 2);

            graphics.DrawRectangles(new Pen(prediction.Label.Color, 1),
                new[] { prediction.Rectangle });

            var (x, y) = (prediction.Rectangle.X - 3, prediction.Rectangle.Y - 23);

            if (y < 1)
                y += prediction.Rectangle.Height;

            graphics.DrawString($"{prediction.Label.Name} ({score})",
                new Font("Consolas", 16, GraphicsUnit.Pixel), new SolidBrush(prediction.Label.Color),
                new PointF(x, y));
        }
        */
    }
}