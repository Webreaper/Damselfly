using Damselfly.Core.Constants;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.DbModels.Images;

public class ThumbConfig : IThumbConfig
{
    public ThumbSize size { get; set; }
    public bool useAsSource { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public bool cropToRatio { get; set; } = false;
    public bool batchGenerate { get; set; } = true;
}