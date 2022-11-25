using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageClassification.ImageDataStructures;
using Microsoft.ML;
using static ImageClassification.ModelScorer.ConsoleHelpers;
using static ImageClassification.ModelScorer.ModelHelpers;

namespace ImageClassification.ModelScorer;

public class TFModelScorer
{
    private static string ImageReal = nameof(ImageReal);
    private readonly string imagesFolder;
    private readonly string labelsLocation;
    private readonly MLContext mlContext;
    private readonly string modelLocation;

    public TFModelScorer(string imagesFolder, string modelLocation, string labelsLocation)
    {
        this.imagesFolder = imagesFolder;
        this.modelLocation = modelLocation;
        this.labelsLocation = labelsLocation;
        mlContext = new MLContext();
    }

    public void Score(List<ImageNetData> images)
    {
        var model = LoadModel(imagesFolder, modelLocation);

        var predictions = PredictDataUsingModel(images, labelsLocation, model).ToArray();
    }

    private PredictionEngine<ImageNetData, ImageNetPrediction> LoadModel(string imagesFolder, string modelLocation)
    {
        ConsoleWriteHeader("Read model");
        Console.WriteLine($"Model location: {modelLocation}");
        Console.WriteLine(
            $"Default parameters: image size=({ImageNetSettings.imageWidth},{ImageNetSettings.imageHeight}), image mean: {ImageNetSettings.mean}");

        var imageData = Directory.GetFiles(imagesFolder, "*_m.jpg")
            .Select(x => new ImageNetData { ImagePath = x })
            .Take(0);

        var data = mlContext.Data.LoadFromEnumerable(imageData);

        ConsoleWriteHeader("Load images and transform/score");
        var pipeline = mlContext.Transforms.LoadImages("input", imagesFolder, nameof(ImageNetData.ImagePath))
            .Append(mlContext.Transforms.ResizeImages("input", ImageNetSettings.imageWidth,
                ImageNetSettings.imageHeight, "input"))
            .Append(mlContext.Transforms.ExtractPixels("input", interleavePixelColors: ImageNetSettings.channelsLast,
                offsetImage: ImageNetSettings.mean))
            .Append(mlContext.Model.LoadTensorFlowModel(modelLocation).ScoreTensorFlowModel(new[] { "softmax2" },
                new[] { "input" }, true));

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

        var labels = ReadLabels(labelsLocation);

        foreach ( var sample in imageData )
        {
            var probs = model.Predict(sample).PredictedLabels;
            var result = new ImageNetDataProbability
            {
                ImagePath = sample.ImagePath
            };
            (result.PredictedLabel, result.Probability) = GetBestLabel(labels, probs);

            if ( result.Probability > 0.4f )
            {
                result.ConsoleWrite();
                yield return result;
            }
        }
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
}