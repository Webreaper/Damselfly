using System;
namespace Damselfly.Core.Utils.Images
{
    public class ImageProcessResult
    {
        public bool ThumbsGenerated { get; set; }
        public string? ImageHash { get; set; }
        public string? PerceptualHash { get; set; }
    }
}

