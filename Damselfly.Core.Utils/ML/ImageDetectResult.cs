using System.Drawing;

namespace Damselfly.Core.Utils.ML;

public class ImageDetectResult
{
    public Rectangle Rect { get; set; }
    public string Tag { get; set; }
    public string Service { get; set; }
    public string ServiceModel { get; set; }
    public float Score { get; set; }
    public float[] Embeddings { get; set; }
    public bool IsNewPerson { get; set; }
    public string PersonGuid { get; set; }
    public bool IsFace => string.Compare(Tag, "Face", true) == 0;
}