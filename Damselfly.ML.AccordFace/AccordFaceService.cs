using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Accord.Vision.Detection;
using Damselfly.Core.Utils;
using Damselfly.Shared.Utils;
using Damselfly.Core.Utils.ML;

namespace Damselfly.ML.Face.Accord
{
    public class AccordFaceService
    {
        private float ScaleFactor { get; set; } = 1.2f;
        private int MinSize { get; set; } = 10;
        private ObjectDetectorScalingMode ScaleMode { get; set; } = ObjectDetectorScalingMode.GreaterToSmaller;
        private ObjectDetectorSearchMode SearchMode { get; set; } = ObjectDetectorSearchMode.Average;
        private bool Parallel { get; set; } = false;
        private int Suppression { get; set; } = 3;
        private FaceDetector _faceDetector;

        public AccordFaceService()
        {
             _faceDetector = new FaceDetector();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public List<ImageDetectResult> DetectFaces( Bitmap inputImage )
        {
            var result = new List<ImageDetectResult>();

            try
            {
                var watch = new Stopwatch("AccordFace");

                // Need the resize - although it makes the detection rubbish.
                var processedImage = new ImageProcessor(inputImage).Grayscale().EqualizeHistogram().Resize(640, out var ratio).Result;

                var faces = DoFaceRecognition(processedImage);

                // Flip the ratio as we're scaling not shrinking now.
                ratio = 1 / ratio;

                result = faces.Select( x => new ImageDetectResult
                {
                    Rect = new Rectangle
                    {
                        X = (int)(x.X * ratio),
                        Y = (int)(x.Y * ratio),
                        Width = (int)(x.Width * ratio),
                        Height = (int)(x.Height * ratio)
                    },
                    Tag = "Face",
                    Service = "Accord.Net",
                    ServiceModel = "HarrClassifier"
                }).ToList();

                watch.Stop();   
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during Accord face detection pre-processing: {ex.Message}");
            }

            return result;
        }

        private List<Rectangle> DoFaceRecognition(Bitmap processedImage)
        {
            try
            {
                return _faceDetector.ExtractFaces(processedImage,
                    FaceDetectorParameters.Create(ScaleFactor, MinSize, ScaleMode, SearchMode, Parallel, Suppression));
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during Accord Face extraction: {ex}");
                return new List<Rectangle>();
            }
        }
    }
}
