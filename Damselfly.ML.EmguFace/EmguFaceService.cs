using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Humanizer;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.ML;
using Damselfly.Core.Interfaces;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.ImgHash;

namespace Damselfly.ML.Face.Emgu
{
    public class EmguFaceService : IHashProvider
    {
        private class ClassifierModel
        {
            public CascadeClassifier Classifier { get; private set; }
            public string ClassifierFile { get; private set; }
            public string ClassifierTag { get; private set; }

            public ClassifierModel( string file, string tag, CascadeClassifier classifier )
            {
                Classifier = classifier;
                ClassifierFile = file;
                ClassifierTag = tag;
            }
        }

        private List<ClassifierModel> classifiers = new List<ClassifierModel>();
        public bool ServiceAvailable { get { return classifiers.Any(); } }

        public EmguFaceService()
        {
            InitialiseClassifiers();
        }
        

        /// <summary>
        /// Created a perceptual hash for an image
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Binary string of the hash</returns>
        public string GetPerceptualHash( string path )
        {
            using var img = CvInvoke.Imread(path);
            var hashAlgorithm = new MarrHildrethHash();
            var hash = new Mat();
            hashAlgorithm.Compute(img, hash);

            // Get the data from the unmanage memeory
            var data = new byte[hash.Width * hash.Height];
            Marshal.Copy(hash.DataPointer, data, 0, hash.Width * hash.Height);

            // Concatenate the Hex values representation
            string hexString = BitConverter.ToString(data);

            Logging.LogVerbose($"EMGU created a hash: {hexString}");

            return hexString.Replace( "-", "");
        }

        private bool IsSupported
        {
            get
            {
                return true;
            }
        }

        private void InitialiseClassifiers()
        {
            if (! IsSupported)
                return;

            var modelDir = MLUtils.ModelFolder;

            if (modelDir != null)
            {
                var haarcascades = modelDir.GetFiles("haarcascade*.xml", SearchOption.AllDirectories).ToList();

                foreach (var modelFile in haarcascades)
                {
                    try
                    {
                        var tag = modelFile.Directory?.Name.Transform(To.SentenceCase);

                        if (string.IsNullOrEmpty(tag))
                            tag = "Object";

                        var classifier = new CascadeClassifier(modelFile.FullName);
                        var model = new ClassifierModel(modelFile.Name, tag, classifier);

                        Logging.Log($"Initialised EMGU classifier with {model.ClassifierFile} for tag '{tag}'");
                        classifiers.Add( model );
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError($"Unable to initialize Emgu face detection: {ex}");
                    }
                }
            }
        }

        public List<ImageDetectResult> DetectFaces( string path )
        {
            var result = new List<ImageDetectResult>();

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Comment from EMGUCV:
                    // Mac OS’s security policy has been changing quite a bit in the last few OS releases, it
                    // may have blocked access to the temporary directory that Open CV used to cache OpenCL kernels.
                    // You can probably call “CvInvoke.UseOpenCL = false” to disable OpenCL if the running platform
                    // is MacOS.It will disable Open CL and no kernels will be cached on disk.Not an ideal solution,
                    // but unless Open CV address this on future release(or Apple stop changing security policies that
                    // often) this will be a problem for Mac.
                    CvInvoke.UseOpenCL = false;
                }

                //var img = bitmap.ToMat();
                using var img = CvInvoke.Imread(path);
                using var imgGray = new UMat();
                CvInvoke.CvtColor(img, imgGray, ColorConversion.Bgr2Gray);
                CvInvoke.EqualizeHist(imgGray, imgGray);

                foreach ( var model in classifiers )
                {
                    var faces = model.Classifier.DetectMultiScale(imgGray, 1.1, 10, new Size(20, 20), Size.Empty).ToList();

                    if ( faces.Any() )
                    {
                        result.AddRange( faces.Distinct()
                                              .Select(x => new ImageDetectResult
                        {
                            Rect = x,
                            Tag = model.ClassifierTag.Transform(To.SentenceCase),
                            Service = "Emgu",
                            ServiceModel = model.ClassifierFile
                        }));
                    }
                }

                return DetectDupeRects( result );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }

            return result;
        }

        public List<ImageDetectResult> DetectDupeRects( List<ImageDetectResult> results )
        {
            var toDelete = new HashSet<int>();

            for( int i = 0; i < results.Count; i++ )
            {
                for( int j = 0; j < results.Count; j++ )
                {
                    if (i == j || toDelete.Contains(i))
                    {
                        // If the first rect we're comparing with is already
                        // slated for deletion, don't bother checking again.
                        continue;
                    }

                    var firstRect = results[i].Rect;
                    var secondRect = results[j].Rect;

                    Rectangle rect = Rectangle.Intersect(firstRect, secondRect);
                    var percentage = (rect.Width * rect.Height) * 100f / (firstRect.Width * firstRect.Height);

                    // If the second of the pair overlaps the first by 90% and the first isn't
                    // already scheduled to be deleted, add it to the to-delete list.
                    if ( percentage > 90 )
                    {
                        Logging.LogVerbose($"Removing dupe face rect with 90% intersect: [{firstRect} / {secondRect}]");
                        toDelete.Add(j);
                    }
                }
            }

            var newResults = new List<ImageDetectResult>();

            for( int i = 0; i < results.Count; i++ )
            {
                if (!toDelete.Contains(i))
                    newResults.Add( results[i] );
            }

            return newResults;
        }
    }
}
