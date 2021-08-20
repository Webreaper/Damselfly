using System;
using Emgu.CV;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV.CvEnum;
using System.IO;
using System.Linq;
using Humanizer;
using Damselfly.Core.Utils;
using System.Collections.Generic;
using Damselfly.Core.Utils.ML;
using System.Reflection;

namespace Damselfly.ML.Face.Emgu
{
    public class EmguFaceService
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

       

        private bool IsSupported
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var asm = Assembly.GetAssembly(typeof(CascadeClassifier));

                    if (asm != null)
                    {
                        var ver = asm.GetName().Version;
                        if (ver != null)
                        {
                            var reqVer = new Version(4, 5, 3, 0);

                            if (ver < reqVer)
                            {
                                // No MacOS love for EMGU until this bug is fixed in 4.5.3
                                // https://github.com/emgucv/emgucv/issues/550
                                Logging.Log("EMGU not available on OSX.");
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }
        private void InitialiseClassifiers()
        {
            if (! IsSupported)
                return;

            var modelDir = MLUtils.GetModelFolder();

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
                foreach( var model in classifiers )
                {
                    //var img = bitmap.ToMat();
                    using var img = CvInvoke.Imread(path);
                    using var imgGray = new UMat();
                    CvInvoke.CvtColor(img, imgGray, ColorConversion.Bgr2Gray);

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

                        DetectDupeRects( ref result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }

            return result;
        }

        public void DetectDupeRects( ref List<ImageDetectResult> results )
        {
            var toDelete = new List<int>();

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

            // Now, remove the items, last-first so the collection
            // indexes don't change. 
            foreach( var del in toDelete.OrderByDescending( x => x ) )
            {
                results.RemoveAt( del ); 
            }
        }
    }
}
