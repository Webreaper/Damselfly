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
        public List<Face> DetectFaces( Bitmap inputImage )
        {
            var faces = new List<Face>();

            try
            {
                var watch = new Stopwatch("AccordFace");

                var processedImage = new ImageProcessor(inputImage).Resize( new Size( 320, 320 ) ).Grayscale().EqualizeHistogram().Result;

                faces = DoFaceRecognition(processedImage);

                watch.Stop();   
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during Accord face detection pre-processing: {ex.Message}");
            }

            return faces;
        }

        private List<Face> DoFaceRecognition(Bitmap processedImage)
        {
            try
            {
                return _faceDetector.ExtractFaces(processedImage,
                    FaceDetectorParameters.Create(ScaleFactor, MinSize, ScaleMode, SearchMode, Parallel, Suppression));
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during Accord Face extraction: {ex}");
                return new List<Face>();
            }
        }
    }
}
