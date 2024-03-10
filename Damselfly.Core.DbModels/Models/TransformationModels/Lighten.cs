using Damselfly.Core.Constants;

namespace Damselfly.Core.DbModels.Models.TransformationModels;

public class Lighten : ITransform
{
    public int Order => 3;

    public double LighteningFactor { get; set; }
}

