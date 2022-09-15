using Microsoft.ML.Data;

namespace ImageClassification.ImageDataStructures;

public class ImageNetData
{
    [LoadColumn(0)] public string ImagePath;
}

public class ImageNetDataProbability : ImageNetData
{
    public string PredictedLabel;
    public float Probability { get; set; }
}