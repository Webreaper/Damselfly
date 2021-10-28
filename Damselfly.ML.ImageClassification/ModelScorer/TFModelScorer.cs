using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using ImageClassification.ImageDataStructures;
using static ImageClassification.ModelScorer.ConsoleHelpers;
using static ImageClassification.ModelScorer.ModelHelpers;

namespace ImageClassification.ModelScorer
{
    public class TFModelScorer
    {
        private readonly string imagesFolder;
        private readonly string modelLocation;
        private readonly string labelsLocation;
        private readonly MLContext mlContext;
        private static string ImageReal = nameof(ImageReal);

        public TFModelScorer(string imagesFolder, string modelLocation, string labelsLocation)
        {
            this.imagesFolder = imagesFolder;
            this.modelLocation = modelLocation;
            this.labelsLocation = labelsLocation;
            mlContext = new MLContext();
        }

        public struct ImageNetSettings
        {
            public const int imageHeight = 224;
            public const int imageWidth = 224;
            public const float mean = 117;
            public const bool channelsLast = true;
        }

        public struct InceptionSettings
        {
            // for checking tensor names, you can use tools like Netron,
            // which is installed by Visual Studio AI Tools

            // input tensor name
            public const string inputTensorName = "input";

            // output tensor name
            public const string outputTensorName = "softmax2";
        }

        public void Score( List<ImageNetData> images )
        {
            var model = LoadModel(imagesFolder, modelLocation);

            var predictions = PredictDataUsingModel(images, labelsLocation, model).ToArray();

        }

        private PredictionEngine<ImageNetData, ImageNetPrediction> LoadModel(string imagesFolder, string modelLocation)
        {
            ConsoleWriteHeader("Read model");
            Console.WriteLine($"Model location: {modelLocation}");
            Console.WriteLine($"Default parameters: image size=({ImageNetSettings.imageWidth},{ImageNetSettings.imageHeight}), image mean: {ImageNetSettings.mean}");

            var imageData = System.IO.Directory.GetFiles(imagesFolder, "*_m.jpg")
                                               .Select(x => new ImageNetData { ImagePath = x })
                                               .Take(0);

            var data = mlContext.Data.LoadFromEnumerable<ImageNetData>(imageData);

            ConsoleWriteHeader("Load images and transform/score");
            var pipeline = mlContext.Transforms.LoadImages(outputColumnName: "input", imageFolder: imagesFolder, inputColumnName: nameof(ImageNetData.ImagePath))
                            .Append(mlContext.Transforms.ResizeImages(outputColumnName: "input", imageWidth: ImageNetSettings.imageWidth, imageHeight: ImageNetSettings.imageHeight, inputColumnName: "input"))
                            .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input", interleavePixelColors: ImageNetSettings.channelsLast, offsetImage: ImageNetSettings.mean))
                            .Append(mlContext.Model.LoadTensorFlowModel(modelLocation).
                            ScoreTensorFlowModel(outputColumnNames: new[] { "softmax2" },
                                                 inputColumnNames: new[] { "input" }, addBatchDimensionInput:true));

            ConsoleWriteHeader("Pipeline Fit");
            ITransformer model = pipeline.Fit(data);

            ConsoleWriteHeader("Create Engine");
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ImageNetData, ImageNetPrediction>(model);

            return predictionEngine;
        }

        protected IEnumerable<ImageNetData> PredictDataUsingModel(List<ImageNetData> imageData, 
                                                                  string labelsLocation, 
                                                                  PredictionEngine<ImageNetData, ImageNetPrediction> model)
        {
            ConsoleWriteHeader("Classify images");
            Console.WriteLine($"Images folder: {imagesFolder}");
            Console.WriteLine($"Labels file: {labelsLocation}");

            var labels = ModelHelpers.ReadLabels(labelsLocation);

            foreach (var sample in imageData)
            {
                var probs = model.Predict(sample).PredictedLabels;
                var result = new ImageNetDataProbability()
                {
                    ImagePath = sample.ImagePath,
                };
                (result.PredictedLabel, result.Probability) = GetBestLabel(labels, probs);

                if (result.Probability > 0.4f)
                {
                    result.ConsoleWrite();
                    yield return result;
                }
            }
        }
    }
}
