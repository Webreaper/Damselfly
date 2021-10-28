using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.ML;
using Damselfly.ML.ImageClassification.DominantColors;
using ImageClassification.ModelScorer;

namespace Damselfly.ML.ImageClassification
{
    public class ImageClassifier
    {
        public Color DetectDominantColour( Bitmap image )
        {
            var calculator = new DominantHueColorCalculator();

            var color = calculator.CalculateDominantColor(image);

            return color;
        }

        public Color DetectAverageColor(Bitmap image)
        {
            var calculator = new DominantHueColorCalculator();

            var average = DominantColorUtils.GetAverageRGBColor(image);

            return average;
        }

        public async Task<ImageDetectResult> DetectObjects(Bitmap image)
        {
            var modelDir = MLUtils.ModelFolder;

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
