
namespace Damselfly.Core.DbModels.Models.TransformationModels;

public class CropTransform : ITransform
{
    public int Order => 1;

    // Origin and crop size, in pixels
    public int Top { get; set; }
    public int Left { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
