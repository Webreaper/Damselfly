using ImageClassification.ModelScorer;
using System;
using System.IO;


namespace ImageClassification
{
    public class Program
    {
        static void Main(string[] args)
        {
            string assetsRelativePath = @"../../../assets";
            string assetsPath = GetAbsolutePath(assetsRelativePath);

            //var imagesFolder = Path.Combine(assetsPath, "inputs", "images");
            var imagesFolder = "/Users/markotway/LocalCloud/Development/Damselfly/Damselfly.Web/config/thumbs/Mark Phone";
            var inceptionPb = Path.Combine(assetsPath, "inception", "tensorflow_inception_graph.pb");
            var labelsTxt = Path.Combine(assetsPath, "inception", "imagenet_comp_graph_label_strings.txt");

            try
            {
                var modelScorer = new TFModelScorer(imagesFolder, inceptionPb, labelsTxt);
                modelScorer.Score();

            }
            catch (Exception ex)
            {
                ConsoleHelpers.ConsoleWriteException(ex.ToString());
            }

            ConsoleHelpers.ConsolePressAnyKey();
        }

        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(Program).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;
            string fullPath = Path.Combine(assemblyFolderPath, relativePath);
            return fullPath;
        }
    }
}
