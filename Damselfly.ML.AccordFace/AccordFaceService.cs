using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private bool Parallel { get; set; } = true;
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

                faces = _faceDetector.ExtractFaces(
                    new ImageProcessor(pic).Grayscale().EqualizeHistogram().Result,
                    FaceDetectorParameters.Create(ScaleFactor, MinSize, ScaleMode, SearchMode, Parallel, Suppression))
                    .ToList();

                watch.Stop();

                if (faces.Any())
                    Logging.Log($"Accord found {faces.Count} faces in {watch.ElapsedTime}ms.");
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during face detection: {ex.Message}");
            }

            return faces;
        }
    }
}
