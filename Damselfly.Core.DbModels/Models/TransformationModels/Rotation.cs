using Damselfly.Core.Constants;

namespace Damselfly.Core.DbModels.Models.TransformationModels;

public class Rotation : ITransform
{
    public int Order => 2;

    public OrientationType Orientation { get; set; }
}
