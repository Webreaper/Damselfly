using System;
using Damselfly.Core.Constants;

namespace Damselfly.Core.Interfaces;

public interface IImageProcessResult
{
    bool ThumbsGenerated { get; set; }
    string? ImageHash { get; set; }
    string? PerceptualHash { get; set; }
}

public interface IThumbConfig
{
    public ThumbSize size { get; }
    public bool useAsSource { get; }
    public int width { get; }
    public int height { get; }
    public bool cropToRatio { get; }
    public bool batchGenerate { get; }
}

