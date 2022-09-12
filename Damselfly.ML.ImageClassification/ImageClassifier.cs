using System;
using System.IO;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.ML;
using Damselfly.ML.ImageClassification.DominantColors;
using SixLabors.ImageSharp;
using Humanizer;
using ImageClassification.ModelScorer;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using Tensorflow.Keras.Layers;
using System.Drawing.Imaging;

namespace Damselfly.ML.ImageClassification
{
    public class ImageClassifier
    {
        public System.Drawing.Color DetectDominantColour(string inPath)
        {
            using var image = Image.Load<Rgb24>( inPath );

            image.Mutate(
                x => x
                // Scale the image down preserving the aspect ratio. This will speed up quantization.
                // We use nearest neighbor as it will be the fastest approach.
                .Resize( new ResizeOptions() { Sampler = KnownResamplers.NearestNeighbor, Size = new Size( 100, 0 ) } )

                // Reduce the color palette to 1 color without dithering.
                .Quantize( new OctreeQuantizer( new QuantizerOptions { Dither = null, MaxColors = 1 } ) ) );

            Rgb24 dominant = image[0, 0];

            return System.Drawing.Color.FromArgb( dominant.R, dominant.G, dominant.B );
        }

        public System.Drawing.Color DetectAverageColor( string inPath )
        {
            int totalRed = 0;
            int totalGreen = 0;
            int totalBlue = 0;

            using var image = Image.Load<Rgb24>( inPath );

            image.Mutate( x => x.Resize( new ResizeOptions { Sampler = KnownResamplers.NearestNeighbor, Size = new Size( 100, 0 ) } ) );

            image.ProcessPixelRows( pixelAccessor =>
            {
                for( var y = 0; y < pixelAccessor.Height; y++ )
                {
                    var row = pixelAccessor.GetRowSpan( y );

                    for( var x = 0; x < row.Length; x++ )
                    {
                        totalRed += row[x].R;
                        totalGreen += row[x].G;
                        totalBlue += row[x].B;
                    }
                }
            } );

            int totalPixels = image.Width * image.Height;
            byte avgRed = (byte)( totalRed / totalPixels );
            byte avgGreen = (byte)( totalGreen / totalPixels );
            byte avgBlue = (byte)( totalBlue / totalPixels );
            return System.Drawing.Color.FromArgb( avgRed, avgGreen, avgBlue );
        }

        public ImageDetectResult DetectObjects()
        {
            var modelDir = MLUtils.ModelFolder;
            if (modelDir == null)
            {
                Logging.LogError($"Image classification modelDire was null.");
                return null;
            }

            ImageDetectResult result = null;

            var inceptionPb = Path.Combine(modelDir.FullName, "tensorflow_inception_graph.pb");
            var labelsTxt = Path.Combine(modelDir.FullName, "imagenet_comp_graph_label_strings.txt");

            if (!File.Exists(inceptionPb))
            {
                Logging.LogError($"Image classification TF model was not found at {inceptionPb}");
                return null;
            }
            if (!File.Exists(inceptionPb))
            {
                Logging.LogError($"Image classification TF labels not found at {labelsTxt}");
                return null;
            }

            try
            {
                var imagesFolder = string.Empty; // TODO
                var modelScorer = new TFModelScorer(imagesFolder, inceptionPb, labelsTxt);
                modelScorer.Score(null);

            }
            catch (Exception ex)
            {
                Logging.LogError($"Exception during image classification processing: {ex}");
            }

            return result;
        }
    }
}
