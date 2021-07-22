using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Accord.Vision.Detection;
using Damselfly.Core.Utils;

namespace Damselfly.ML.Face.Accord
{
    public class AccordFaceService
    {
        private float ScaleFactor { get; set; } = 1.2f;
        private int MinSize { get; set; } = 20;
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
        public List<Face> DetectFaces( FileInfo inputFile )
        {
            var faces = new List<Face>();

            try
            {
                var watch = new Stopwatch("AccordFace");

                using var pic = new Bitmap(inputFile.FullName);

                var processedImage = new ImageProcessor(pic).Resize( new Size( 320, 320 ) ).Grayscale().EqualizeHistogram().Result;
                faces = _faceDetector.ExtractFaces( processedImage,
                    FaceDetectorParameters.Create(ScaleFactor, MinSize, ScaleMode, SearchMode, Parallel, Suppression));

                watch.Stop();   

                if (faces.Any())
                {
                    Logging.Log($"Accord.Net found {faces.Count} faces in {inputFile.Name} in {watch.ElapsedTime}ms.");
                }
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during Accord face detection: {ex.Message}");
            }

            return faces;
        }
    }
}
