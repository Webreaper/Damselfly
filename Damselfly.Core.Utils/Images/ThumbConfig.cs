
namespace Damselfly.Core.Utils.Images
{
    public enum ThumbSize
    {
        Unknown = -1,
        ExtraLarge = 0,
        Large = 1,
        Big = 2,
        Medium = 3,
        Preview = 4,
        Small = 5
    };

    public class ThumbConfig
    {
        public ThumbSize size;
        public bool useAsSource;
        public int width;
        public int height;
        public bool cropToRatio = false;
        public bool batchGenerate = true;
    }
}
