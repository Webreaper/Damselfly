using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.ML;
using Yolov5Net.Scorer;
using Yolov5Net.Scorer.Models;

namespace Damselfly.ML.ObjectDetection
{
    public class ObjectDetector
    {
        private YoloScorer<YoloCocoModel> scorer;
        const float predictionThreshold = 0.5f;

        public ObjectDetector()
        {
        }

        public void InitScorer()
        {
            Logging.Log("Initialising ObjectDetector service.");
            try
            {
                scorer = new YoloScorer<YoloCocoModel>();
            }
            catch (Exception ex)
            {
                Logging.LogError($"Unexpected exception initialising Object detection: {ex}");
                scorer = null; // disable.
            }
        }

        /// <summary>
        /// Given an image, detect objects in it using the Yolo v5 model.
        /// </summary>
        /// <param name="imageFile"></param>
        /// <returns></returns>
        public async Task<IList<ImageDetectResult>> DetectObjects( Bitmap image )
        {
            try
            {

                if (scorer == null)
                    return new List<ImageDetectResult>();

                var result = await Task.Run(() =>
                {
                    Stopwatch watch = new Stopwatch("DetectObjects");

                    var predictions = scorer.Predict(image);

                    watch.Stop();

                    return predictions.Where( x => x.Score > predictionThreshold)
                                      .Select(x => MakeResult(x))
                                      .ToList();
                });

                return result;
            }
            catch( Exception ex )
            {
                Logging.LogError($"Error during object detection: {ex.Message}");
            }

            return new List<ImageDetectResult>();
        }

        private ImageDetectResult MakeResult( YoloPrediction prediction )
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

        private void DrawRectangles( Image img, List<YoloPrediction> predictions )
        {
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
        }
    }
}
