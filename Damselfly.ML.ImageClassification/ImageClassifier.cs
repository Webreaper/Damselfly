using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Damselfly.Core.Utils.ML;
using ImageClassification.ModelScorer;

namespace Damselfly.ML.ImageClassification
{
    public class ImageClassfier
    {
        public async Task<ImageDetectResult> DetectObjects(Bitmap image)
        {

            var modelDir = MLUtils.GetModelFolder();

            ImageDetectResult result = null;

            var inceptionPb = Path.Combine(modelDir.FullName, "tensorflow_inception_graph.pb");
            var labelsTxt = Path.Combine(modelDir.FullName, "imagenet_comp_graph_label_strings.txt");

            try
            {
                var imagesFolder = string.Empty; // TODO
                var modelScorer = new TFModelScorer(imagesFolder, inceptionPb, labelsTxt);
                modelScorer.Score();

            }
            catch (Exception ex)
            {
                ConsoleHelpers.ConsoleWriteException(ex.ToString());
            }

            ConsoleHelpers.ConsolePressAnyKey();

            return result;
        }
    }
}
