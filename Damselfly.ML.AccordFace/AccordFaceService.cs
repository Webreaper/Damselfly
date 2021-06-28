using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Accord.Vision.Detection;
using Damselfly.Core.Utils;

namespace Damselfly.ML.Accord.Face
{
    public static class AccordFaceService
    {
        public static float ScaleFactor { get; set; } = 1.2f;
        public static int MinSize { get; set; } = 20;
        public static ObjectDetectorScalingMode ScaleMode { get; set; } = ObjectDetectorScalingMode.GreaterToSmaller;
        public static ObjectDetectorSearchMode SearchMode { get; set; } = ObjectDetectorSearchMode.Average;
        public static bool Parallel { get; set; } = true;
        public static int Suppression { get; set; } = 3;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static List<Face> DetectFaces( FileInfo inputFile )
        {
            FaceDetector _faceDetector = new FaceDetector();
            using var pic = new Bitmap( inputFile.FullName );

            var faces = _faceDetector.ExtractFaces(
                new ImageProcessor(pic).Grayscale().EqualizeHistogram().Result,
                FaceDetectorParameters.Create(ScaleFactor, MinSize, ScaleMode, SearchMode, Parallel, Suppression))
                .ToList();

#if DEBUG
            if (faces.Any())
            {

                if (System.Diagnostics.Debugger.IsAttached)
                {
                    string outDir = "/Users/markotway/Desktop/Faces";
                    if (!Directory.Exists(outDir))
                        Directory.CreateDirectory(outDir);

                    var output = Path.Combine(outDir, inputFile.Name);
                    using (Graphics G = Graphics.FromImage(pic))
                    {
                        faces.ForEach(x => G.DrawRectangle(Pens.Red, x.FaceRectangle));
                    }

                    pic.Save(output, ImageFormat.Bmp);

                    Logging.Log($"Found {faces.Count} faces:");
                    faces.ForEach(x => Logging.Log($" Face Rectangle: {x.FaceRectangle}"));
                }
            }
#endif

            return faces;
        }
    }
}
