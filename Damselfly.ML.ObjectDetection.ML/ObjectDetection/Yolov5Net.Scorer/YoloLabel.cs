using System.Drawing;

namespace Yolov5Net.Scorer;

/// <summary>
///     Label of detected object.
/// </summary>
public class YoloLabel
{
    public YoloLabel()
    {
        Color = Color.Yellow;
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public YoloLabelKind Kind { get; set; }
    public Color Color { get; set; }
}