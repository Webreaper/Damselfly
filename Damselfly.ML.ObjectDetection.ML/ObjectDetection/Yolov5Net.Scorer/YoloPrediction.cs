using SixLabors.ImageSharp;

namespace Yolov5Net.Scorer;

/// <summary>
///     Object prediction.
/// </summary>
public class YoloPrediction
{
    public YoloPrediction()
    {
    }

    public YoloPrediction(YoloLabel label, float confidence) : this(label)
    {
        Score = confidence;
    }

    public YoloPrediction(YoloLabel label)
    {
        Label = label;
    }

    public YoloLabel Label { get; set; }
    public RectangleF Rectangle { get; set; }
    public float Score { get; set; }
}