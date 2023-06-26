using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.ML;
using Damselfly.Shared.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Yolov5Net.Scorer;
using Yolov5Net.Scorer.Models;
using Rectangle = System.Drawing.Rectangle;

namespace Damselfly.ML.ObjectDetection;

public class ObjectDetector
{
    private const float predictionThreshold = 0.5f;
    private YoloScorer<YoloCocoModel> scorer;

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
    public Task<IList<ImageDetectResult>> DetectObjects(Image<Rgb24> image)
    {
        IList<ImageDetectResult> result = null;
        try
        {
            if( scorer != null )
            {

                var watch = new Stopwatch( "DetectObjects" );

                var predictions = scorer.Predict( image );

                watch.Stop();

                var objectsFound = predictions.Where( x => x.Score > predictionThreshold )
                    .Select( x => MakeResult( x ) )
                    .ToList();

                result = objectsFound;

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

    private ImageDetectResult MakeResult(YoloPrediction prediction)
    {
        return new ImageDetectResult
        {
            Rect = new Rectangle
            {
                X = (int)prediction.Rectangle.X,
                Y = (int)prediction.Rectangle.Y,
                Width = (int)prediction.Rectangle.Width,
                Height = (int)prediction.Rectangle.Height
            },
            Tag = prediction.Label.Name,
            Service = "ML.Net",
            ServiceModel = "ONNX/Yolo"
        };
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