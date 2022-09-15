using Damselfly.Core.Interfaces;

namespace Damselfly.Core.DbModels.Images;

public class ImageProcessResult : IImageProcessResult
{
    public bool ThumbsGenerated { get; set; }
    public string? ImageHash { get; set; }
    public string? PerceptualHash { get; set; }
}