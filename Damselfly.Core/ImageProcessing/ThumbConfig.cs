using System;
namespace Damselfly.Core.ImageProcessing
{
    public enum ThumbSize
    {
        ExtraLarge,
        Large,
        Big,
        Medium,
        Preview,
        Small
    };

    public class ThumbConfig
    {
        public ThumbSize size;
        public bool useAsSource;
        public int width;
        public int height;
        public bool cropToRatio = false;
    }
}
