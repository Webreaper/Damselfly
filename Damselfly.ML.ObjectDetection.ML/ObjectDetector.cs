using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Yolov5Net.Scorer;
using Yolov5Net.Scorer.Models;

namespace Damselfly.ML.ObjectDetection
{
    public class ObjectDetector
    {
        private readonly YoloScorer<YoloCocoModel> scorer;

        public ObjectDetector()
        {
            Logging.Log("Initialising ObjectDetector service.");
            try
            {
                scorer = new YoloScorer<YoloCocoModel>();
            }
            catch( Exception ex )
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
        public async Task<IList<YoloPrediction>> DetectObjects( FileInfo imageFile )
        {
            var result = new List<YoloPrediction>();
            try
            {

                if (scorer == null)
                    return new List<YoloPrediction>();

                result = await Task.Run(() =>
                {
                    using var stream = new FileStream(imageFile.FullName, FileMode.Open);

                    var img = Image.FromStream(stream);

                    Stopwatch watch = new Stopwatch("DetectObjects");

                    var predictions = scorer.Predict(img);

                    watch.Stop();

                    return predictions;
                });
            }
            catch( Exception ex )
            {
                Logging.LogError($"Error during object detection: {ex.Message}");
            }

            return result;
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
